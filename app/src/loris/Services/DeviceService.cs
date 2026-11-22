using Loris.Data;
using Loris.Logging;
using Loris.Models;
using Loris.Results;
using Loris.Results.Errors;
using loris.Services;
using Microsoft.EntityFrameworkCore;

namespace Loris.Services;

public class DeviceService(
    ILogger<DeviceService> logger,
    LorisDbContext ctx,
    IStateCache stateCache,
    IChirpstackService chirpstackService
) : IDeviceService
{
    private readonly ILogger<DeviceService> _logger = logger;
    private readonly LorisDbContext _ctx = ctx;
    private readonly IStateCache _stateCache = stateCache;
    private readonly IChirpstackService _chirpstackService = chirpstackService;

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
                DeviceErrors.Ignored(device.FriendlyName)
            ),
            DeviceStatus.DISCONNECTED => Result<Device>.Failure(
                DeviceErrors.Disconnected(device.FriendlyName)
            ),
            _ => Result<Device>.Failure(DeviceErrors.UnknownStatus(device.FriendlyName)),
        };
    }

    private async Task<Result<Device>> AcceptDeviceAsync(Device device, CancellationToken ct)
    {
        var downlinkResult = await _chirpstackService.AcceptAsync(device.DevEui, ct);

        if (downlinkResult.IsFailure)
        {
            _logger.DownlinkFailed(device.DevEui, downlinkResult.Error.Description);
            return Result<Device>.Failure(downlinkResult.Error);
        }

        device.DeviceStatus = DeviceStatus.ACTIVE;

        // We don't want to cancel the database save, because
        // we want to persist the device status change.
        await _ctx.SaveChangesAsync(CancellationToken.None);

        // register newly activated device in runtime
        _stateCache[device.DevEui] = device;

        return Result<Device>.Success(device);
    }

    public async Task<Result<Device>> DisconnectAsync(
        DevEui devEui,
        bool force,
        CancellationToken ct = default
    )
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.DevEui == devEui, ct);
        if (device is null && force)
        {
            // we don't have the device in the database - maybe we replaced it with a new device,
            // then we just want to force the disconnection without the businness logic
            var result = await _chirpstackService.DisconnectAsync(devEui, ct);
            return result.Match(
                () => Result<Device>.Success(new Device()), // empty device
                error => Result<Device>.Failure(error)
            );
        }
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
        var downlinkResult = await _chirpstackService.DisconnectAsync(device.DevEui, ct);
        if (downlinkResult.IsFailure)
        {
            _logger.DownlinkFailed(device.DevEui, downlinkResult.Error.Description);
            return Result<Device>.Failure(downlinkResult.Error);
        }

        device.DeviceStatus = DeviceStatus.DISCONNECTED;

        // We don't want to cancel the database save, because
        // we want to persist the device status change.
        await _ctx.SaveChangesAsync(CancellationToken.None);

        // update state cache; device is not ACCEPTED anymore
        _stateCache[device.DevEui] = device;

        return Result<Device>.Success(device);
    }

    public async Task<Result<Device>> ToggleIgnoreAsync(
        DevEui devEui,
        bool ignore,
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
        bool ignore,
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
            (DeviceStatus.IGNORED, true) => Result<Device>.Failure(
                DeviceErrors.AlreadyIgnored(device.FriendlyName)
            ),
            (DeviceStatus.ACTIVE, _) => Result<Device>.Failure(
                DeviceErrors.Active(device.FriendlyName)
            ),
            (DeviceStatus.DISCONNECTED, _) => Result<Device>.Failure(
                DeviceErrors.Disconnected(device.FriendlyName)
            ),
            (DeviceStatus.PENDING, false) => Result<Device>.Failure(
                DeviceErrors.Pending(device.FriendlyName)
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
        _stateCache[device.DevEui] = device;
        return Result<Device>.Success(device);
    }
}
