using Grpc.Core;
using Loris.Data;
using Loris.Extensions;
using Loris.Logging;
using Loris.Models;
using Loris.Protocol;
using Loris.Results;
using Loris.Results.Errors;
using Loris.Types;
using Microsoft.EntityFrameworkCore;
using DownlinkServiceClient = Chirpstack.Api.DeviceService.DeviceServiceClient;

namespace Loris.Services;

public class DeviceService(
    ILogger<DeviceService> logger,
    LorisDbContext ctx,
    IStateCache stateCache,
    DownlinkServiceClient client
) : IDeviceService
{
    private readonly ILogger<DeviceService> _logger = logger;
    private readonly LorisDbContext _ctx = ctx;
    private readonly IStateCache _stateCache = stateCache;
    private readonly DownlinkServiceClient _client = client;

    public async Task<Result<Device>> AcceptAsync(DevEui devEui, CancellationToken ct = default)
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.DevEui == devEui, ct);
        return device is null
            ? Result<Device>.Failure(DeviceErrors.NotFound(devEui))
            : await AcceptAsync(device, ct);
    }

    public async Task<Result<Device>> AcceptAsync(string name, CancellationToken ct = default)
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.FriendlyName == name, ct);
        return device is null
            ? Result<Device>.Failure(DeviceErrors.NotFound(name))
            : await AcceptAsync(device, ct);
    }

    private async Task<Result<Device>> AcceptAsync(Device device, CancellationToken ct = default)
    {
        return device.DeviceStatus switch
        {
            DeviceStatus.PENDING => await AcceptDeviceAsync(device, ct),
            DeviceStatus.ACTIVE => Result<Device>.Failure(
                DeviceErrors.AlreadyActive(device.FriendlyName)
            ),
            DeviceStatus.IGNORED => Result<Device>.Failure(
                DeviceErrors.AlreadyIgnored(device.FriendlyName)
            ),
            DeviceStatus.DISCONNECTED => Result<Device>.Failure(
                DeviceErrors.AlreadyDisconnected(device.FriendlyName)
            ),
            _ => Result<Device>.Failure(DeviceErrors.UnknownStatus(device.FriendlyName)),
        };
    }

    private async Task<Result<Device>> AcceptDeviceAsync(Device device, CancellationToken ct)
    {
        try
        {
            await _client.EnqueueDownlinkAsync(
                new Downlink { Ping = new DownPing { Type = DownPingType.Hello } },
                device.DevEui,
                ct
            );
        }
        catch (RpcException ex)
        {
            if (ex.StatusCode == StatusCode.DeadlineExceeded)
            {
                _logger.DownlinkTimeout(device.DevEui, ex.Message);
                return Result<Device>.Failure(DownlinkErrors.DeadlineExceeded());
            }
            else if (ex.StatusCode == StatusCode.Unauthenticated)
            {
                // TODO: apparently chirpstack is sending unauthenticated, even
                // when the token is valid, but it can't find any device with the given DevEUI.
                // This is a bug in chirpstack and i don't know how to handle it yet.
                //
                _logger.DownlinkFailed(device.DevEui, ex.Message);
                return Result<Device>.Failure(DownlinkErrors.Failed(ex.Message));
            }
            else
            {
                _logger.DownlinkFailed(device.DevEui, ex.Message);
                return Result<Device>.Failure(DownlinkErrors.Failed(ex.Message));
            }
            // TODO: Handle other exceptions if needed
        }

        device.DeviceStatus = DeviceStatus.ACTIVE;

        // We don't want to cancel the database save, because
        // we want to persist the device status change.
        await _ctx.SaveChangesAsync(CancellationToken.None);

        // register newly activated device in runtime
        _stateCache.UpdateDevices(device, accepted: true);

        return Result<Device>.Success(device);
    }

    public async Task<Result<Device>> DisconnectAsync(
        DevEui devEui,
        bool force,
        CancellationToken ct = default
    )
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.DevEui == devEui, ct);
        return device is null
            ? Result<Device>.Failure(DeviceErrors.NotFound(devEui))
            : await DisconnectAsync(device, force, ct);
    }

    public async Task<Result<Device>> DisconnectAsync(
        string name,
        bool force,
        CancellationToken ct = default
    )
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.FriendlyName == name, ct);
        return device is null
            ? Result<Device>.Failure(DeviceErrors.NotFound(name))
            : await DisconnectAsync(device, force, ct);
    }

    private async Task<Result<Device>> DisconnectAsync(
        Device device,
        bool force,
        CancellationToken ct
    )
    {
        return (device.DeviceStatus, force) switch
        {
            (DeviceStatus.ACTIVE, _) => await DisconnectDeviceAsync(device, ct),
            (_, true) => await DisconnectDeviceAsync(device, ct),
            (DeviceStatus.PENDING, false) => Result<Device>.Failure(
                DeviceErrors.Pending(device.FriendlyName)
            ),
            (DeviceStatus.IGNORED, false) => Result<Device>.Failure(
                DeviceErrors.PendingButIgnored(device.FriendlyName)
            ),
            (DeviceStatus.DISCONNECTED, false) => Result<Device>.Failure(
                DeviceErrors.AlreadyDisconnected(device.FriendlyName)
            ),
            _ => Result<Device>.Failure(DeviceErrors.UnknownStatus(device.FriendlyName)),
        };
    }

    private async Task<Result<Device>> DisconnectDeviceAsync(Device device, CancellationToken ct)
    {
        try
        {
            await _client.EnqueueDownlinkAsync(
                new Downlink { Ping = new DownPing { Type = DownPingType.Disconnect } },
                device.DevEui,
                ct
            );
        }
        catch (RpcException ex)
        {
            if (ex.StatusCode == StatusCode.DeadlineExceeded)
            {
                _logger.DownlinkTimeout(device.DevEui, ex.Message);
                return Result<Device>.Failure(DownlinkErrors.DeadlineExceeded());
            }
            else if (ex.StatusCode == StatusCode.Unauthenticated)
            {
                // TODO: apparently chirpstack is sending unauthenticated, even
                // when the token is valid, but it can't find any device with the given DevEUI.
                // This is a bug in chirpstack and i don't know how to handle it yet.
                //
                _logger.DownlinkFailed(device.DevEui, ex.Message);
                return Result<Device>.Failure(DownlinkErrors.Failed(ex.Message));
            }
            else
            {
                _logger.DownlinkFailed(device.DevEui, ex.Message);
                return Result<Device>.Failure(DownlinkErrors.Failed(ex.Message));
            }
            // TODO: Handle other exceptions if needed
        }

        device.DeviceStatus = DeviceStatus.DISCONNECTED;

        // We don't want to cancel the database save, because
        // we want to persist the device status change.
        await _ctx.SaveChangesAsync(CancellationToken.None);

        // State cache remove from ACCEPTED
        _stateCache.UpdateDevices(device, accepted: false);

        return Result<Device>.Success(device);
    }

    public async Task<Result<Device>> ToggleIgnoreAsync(
        DevEui devEui,
        bool ignore = true,
        CancellationToken ct = default
    )
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.DevEui == devEui, ct);
        return device is null
            ? Result<Device>.Failure(DeviceErrors.NotFound(devEui))
            : await ToggleIgnoreAsync(device, ignore, ct);
    }

    public async Task<Result<Device>> ToggleIgnoreAsync(
        string name,
        bool ignore = true,
        CancellationToken ct = default
    )
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.FriendlyName == name, ct);
        return device is null
            ? Result<Device>.Failure(DeviceErrors.NotFound(name))
            : await ToggleIgnoreAsync(device, ignore, ct);
    }

    private async Task<Result<Device>> ToggleIgnoreAsync(
        Device device,
        bool ignore,
        CancellationToken ct
    )
    {
        return (device.DeviceStatus, ignore) switch
        {
            (DeviceStatus.PENDING, true) => await ToggleIgnoreDeviceAsync(
                device,
                DeviceStatus.IGNORED,
                ct
            ),
            (DeviceStatus.IGNORED, false) => await ToggleIgnoreDeviceAsync(
                device,
                DeviceStatus.PENDING,
                ct
            ),
            (DeviceStatus.ACTIVE, _) => Result<Device>.Failure(
                DeviceErrors.AlreadyActive(device.FriendlyName)
            ),
            (DeviceStatus.DISCONNECTED, _) => Result<Device>.Failure(
                DeviceErrors.Disconnected(device.FriendlyName)
            ),
            (DeviceStatus.IGNORED, true) => Result<Device>.Failure(
                DeviceErrors.AlreadyIgnored(device.FriendlyName)
            ),
            (DeviceStatus.PENDING, false) => Result<Device>.Failure(
                DeviceErrors.AlreadyActive(device.FriendlyName)
            ),
            _ => Result<Device>.Failure(DeviceErrors.UnknownStatus(device.FriendlyName)),
        };
    }

    private async Task<Result<Device>> ToggleIgnoreDeviceAsync(
        Device device,
        DeviceStatus status,
        CancellationToken ct
    )
    {
        device.DeviceStatus = status;
        await _ctx.SaveChangesAsync(ct);
        return Result<Device>.Success(device);
    }
}
