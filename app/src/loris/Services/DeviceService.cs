using System.Linq.Expressions;
using Loris.Data;
using Loris.Dtos.Device;
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

    public async Task<Result<IEnumerable<DeviceDetailsResponse>>> ListDevicesAsync(
        List<DeviceStatus> statusFilterList,
        CancellationToken ct = default
    )
    {
        var query = _ctx.Devices.Include(d => d.Sensors).Include(d => d.Functions);
        if (statusFilterList.Count > 0)
        {
            return await query
                .Where(d => statusFilterList.Contains(d.DeviceStatus))
                .Select(d => DeviceDetailsResponse.FromModel(d))
                .ToListAsync(ct);
        }
        return await query.Select(d => DeviceDetailsResponse.FromModel(d)).ToListAsync(ct);
    }

    public async Task<Result<DeviceDetailsResponse>> GetAsync(
        DevEui devEui,
        CancellationToken ct = default
    )
    {
        var device = _ctx
            .Devices.Include(d => d.Functions)
            .Include(d => d.Sensors)
            .FirstOrDefault(d => d.DevEui == devEui);
        if (device is null)
        {
            return DeviceErrors.NotFound(devEui);
        }
        return DeviceDetailsResponse.FromModel(device);
    }

    public async Task<Result<DeviceDetailsResponse>> GetAsync(
        string name,
        CancellationToken ct = default
    )
    {
        var device = _ctx
            .Devices.Include(d => d.Functions)
            .Include(d => d.Sensors)
            .FirstOrDefault(d => d.FriendlyName == name);
        if (device is null)
        {
            return DeviceErrors.NotFound(name);
        }
        return DeviceDetailsResponse.FromModel(device);
    }

    public async Task<Result<DeviceDetailsResponse>> GetAsync(
        Guid deviceId,
        CancellationToken ct = default
    )
    {
        var device = _ctx
            .Devices.Include(d => d.Functions)
            .Include(d => d.Sensors)
            .FirstOrDefault(d => d.DeviceId == deviceId);
        if (device is null)
        {
            return DeviceErrors.NotFound(deviceId);
        }
        return DeviceDetailsResponse.FromModel(device);
    }

    public async Task<Result<DeviceResponse>> RenameAsync(
        DevEui devEui,
        string newName,
        CancellationToken ct = default
    )
    {
        var device = await _ctx.Devices.FirstOrDefaultAsync(d => d.DevEui == devEui, ct);
        if (device is null)
        {
            return DeviceErrors.NotFound(devEui);
        }
        return await RenameAsync(device, newName, ct);
    }

    public async Task<Result<DeviceResponse>> RenameAsync(
        string name,
        string newName,
        CancellationToken ct = default
    )
    {
        // NOTE: THIS IS ALTERNATE APPROACH
        // var result = _ctx
        //     .Devices.Where(d => d.FriendlyName == name)
        //     .ExecuteUpdateAsync(setters => setters.SetProperty(d => d.FriendlyName, newName), ct);
        var device = await _ctx.Devices.FirstOrDefaultAsync(d => d.FriendlyName == name, ct);
        if (device is null)
        {
            return DeviceErrors.NotFound(name);
        }
        return await RenameAsync(device, newName, ct);
    }

    public async Task<Result<DeviceResponse>> RenameAsync(
        Guid deviceId,
        string newName,
        CancellationToken ct = default
    )
    {
        var device = await _ctx.Devices.FirstOrDefaultAsync(d => d.DeviceId == deviceId, ct);
        if (device is null)
        {
            return DeviceErrors.NotFound(deviceId);
        }
        return await RenameAsync(device, newName, ct);
    }

    private async Task<Result<DeviceResponse>> RenameAsync(
        Device device,
        string newName,
        CancellationToken ct
    )
    {
        var existing = await _ctx.Devices.FirstOrDefaultAsync(d => d.FriendlyName == newName, ct);
        if (existing is not null)
        {
            return DeviceErrors.FriendlyNameTaken(newName);
        }
        var oldName = device.FriendlyName;
        device.FriendlyName = newName;
        await _ctx.SaveChangesAsync(ct);
        _stateCache[device.DevEui] = device;
        _logger.DeviceRenamed(device.DeviceId, oldName, newName);
        return DeviceResponse.FromModel(device);
    }

    public async Task<Result<DeviceResponse>> AcceptAsync(
        Guid deviceId,
        CancellationToken ct = default
    )
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.DeviceId == deviceId, ct);
        return device is null ? DeviceErrors.NotFound(deviceId) : await AcceptAsync(device, ct);
    }

    public async Task<Result<DeviceResponse>> AcceptAsync(
        DevEui devEui,
        CancellationToken ct = default
    )
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.DevEui == devEui, ct);
        return device is null ? DeviceErrors.NotFound(devEui) : await AcceptAsync(device, ct);
    }

    public async Task<Result<DeviceResponse>> AcceptAsync(
        string name,
        CancellationToken ct = default
    )
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.FriendlyName == name, ct);
        return device is null ? DeviceErrors.NotFound(name) : await AcceptAsync(device, ct);
    }

    private async Task<Result<DeviceResponse>> AcceptAsync(
        Device device,
        CancellationToken ct = default
    )
    {
        return device.DeviceStatus switch
        {
            DeviceStatus.PENDING => await AcceptDeviceAsync(device, ct),
            DeviceStatus.ACTIVE => DeviceErrors.AlreadyActive(device.FriendlyName),
            DeviceStatus.IGNORED => DeviceErrors.Ignored(device.FriendlyName),
            DeviceStatus.DISCONNECTED => DeviceErrors.Disconnected(device.FriendlyName),
            _ => DeviceErrors.UnknownStatus(device.FriendlyName),
        };
    }

    private async Task<Result<DeviceResponse>> AcceptDeviceAsync(
        Device device,
        CancellationToken ct
    )
    {
        var downlinkResult = await _chirpstackService.AcceptAsync(device.DevEui, ct);

        if (downlinkResult.IsFailure)
        {
            _logger.DownlinkFailed(device.DevEui, downlinkResult.Error.Description);
            return downlinkResult.Error;
        }

        device.DeviceStatus = DeviceStatus.ACTIVE;

        // We don't want to cancel the database save, because
        // we want to persist the device status change.
        await _ctx.SaveChangesAsync(CancellationToken.None);

        // register newly activated device in runtime
        _stateCache[device.DevEui] = device;

        return DeviceResponse.FromModel(device);
    }

    public async Task<Result<DeviceResponse>> DisconnectAsync(
        Guid deviceId,
        bool force,
        CancellationToken ct = default
    )
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.DeviceId == deviceId, ct);
        return device is null
            ? DeviceErrors.NotFound(deviceId)
            : await DisconnectAsync(device, force, ct);
    }

    public async Task<Result<DeviceResponse>> DisconnectAsync(
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
            return result.Match<Result<DeviceResponse>>(
                () => DeviceResponse.FromModel(new Device()), // empty device
                error => error
            );
        }
        return device is null
            ? DeviceErrors.NotFound(devEui)
            : await DisconnectAsync(device, force, ct);
    }

    public async Task<Result<DeviceResponse>> DisconnectAsync(
        string name,
        bool force,
        CancellationToken ct = default
    )
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.FriendlyName == name, ct);
        return device is null
            ? DeviceErrors.NotFound(name)
            : await DisconnectAsync(device, force, ct);
    }

    private async Task<Result<DeviceResponse>> DisconnectAsync(
        Device device,
        bool force,
        CancellationToken ct
    )
    {
        return (device.DeviceStatus, force) switch
        {
            (DeviceStatus.ACTIVE, _) => await DisconnectDeviceAsync(device, ct),
            (_, true) => await DisconnectDeviceAsync(device, ct),
            (DeviceStatus.PENDING, false) => DeviceErrors.Pending(device.FriendlyName),
            (DeviceStatus.IGNORED, false) => DeviceErrors.PendingButIgnored(device.FriendlyName),
            (DeviceStatus.DISCONNECTED, false) => DeviceErrors.AlreadyDisconnected(
                device.FriendlyName
            ),
            _ => DeviceErrors.UnknownStatus(device.FriendlyName),
        };
    }

    private async Task<Result<DeviceResponse>> DisconnectDeviceAsync(
        Device device,
        CancellationToken ct
    )
    {
        var downlinkResult = await _chirpstackService.DisconnectAsync(device.DevEui, ct);
        if (downlinkResult.IsFailure)
        {
            _logger.DownlinkFailed(device.DevEui, downlinkResult.Error.Description);
            return downlinkResult.Error;
        }

        device.DeviceStatus = DeviceStatus.DISCONNECTED;

        // We don't want to cancel the database save, because
        // we want to persist the device status change.
        await _ctx.SaveChangesAsync(CancellationToken.None);

        // update state cache; device is not ACCEPTED anymore
        _stateCache[device.DevEui] = device;

        return DeviceResponse.FromModel(device);
    }

    public async Task<Result<DeviceResponse>> ToggleIgnoreAsync(
        Guid deviceId,
        bool ignore,
        CancellationToken ct = default
    )
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.DeviceId == deviceId, ct);
        return device is null
            ? DeviceErrors.NotFound(deviceId)
            : await ToggleIgnoreAsync(device, ignore, ct);
    }

    public async Task<Result<DeviceResponse>> ToggleIgnoreAsync(
        DevEui devEui,
        bool ignore,
        CancellationToken ct = default
    )
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.DevEui == devEui, ct);
        return device is null
            ? DeviceErrors.NotFound(devEui)
            : await ToggleIgnoreAsync(device, ignore, ct);
    }

    public async Task<Result<DeviceResponse>> ToggleIgnoreAsync(
        string name,
        bool ignore,
        CancellationToken ct = default
    )
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.FriendlyName == name, ct);
        return device is null
            ? DeviceErrors.NotFound(name)
            : await ToggleIgnoreAsync(device, ignore, ct);
    }

    private async Task<Result<DeviceResponse>> ToggleIgnoreAsync(
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
            (DeviceStatus.IGNORED, true) => DeviceErrors.AlreadyIgnored(device.FriendlyName),
            (DeviceStatus.ACTIVE, _) => DeviceErrors.Active(device.FriendlyName),
            (DeviceStatus.DISCONNECTED, _) => DeviceErrors.Disconnected(device.FriendlyName),
            (DeviceStatus.PENDING, false) => DeviceErrors.Pending(device.FriendlyName),
            _ => DeviceErrors.UnknownStatus(device.FriendlyName),
        };
    }

    private async Task<Result<DeviceResponse>> ToggleIgnoreDeviceAsync(
        Device device,
        DeviceStatus status,
        CancellationToken ct
    )
    {
        device.DeviceStatus = status;
        await _ctx.SaveChangesAsync(ct);
        _stateCache[device.DevEui] = device;
        return DeviceResponse.FromModel(device);
    }

    public async Task<Result<DeviceResponse>> ReplaceAsync(
        Guid deviceId,
        DeviceReplaceRequest request,
        CancellationToken ct = default
    )
    {
        var device = await _ctx
            .Devices.Include(d => d.Sensors)
            .Include(d => d.Functions)
            .SingleOrDefaultAsync(d => d.DeviceId == deviceId, ct);
        return device is null
            ? DeviceErrors.NotFound(deviceId)
            : await ReplaceDeviceAsync(device, request, ct);
    }

    public async Task<Result<DeviceResponse>> ReplaceAsync(
        string name,
        DeviceReplaceRequest request,
        CancellationToken ct = default
    )
    {
        var device = await _ctx
            .Devices.Include(d => d.Sensors)
            .Include(d => d.Functions)
            .SingleOrDefaultAsync(d => d.FriendlyName == name, ct);
        return device is null
            ? DeviceErrors.NotFound(name)
            : await ReplaceDeviceAsync(device, request, ct);
    }

    public async Task<Result<DeviceResponse>> ReplaceAsync(
        DevEui devEui,
        DeviceReplaceRequest request,
        CancellationToken ct = default
    )
    {
        var device = await _ctx
            .Devices.Include(d => d.Sensors)
            .Include(d => d.Functions)
            .SingleOrDefaultAsync(d => d.DevEui == devEui, ct);
        return device is null
            ? DeviceErrors.NotFound(devEui)
            : await ReplaceDeviceAsync(device, request, ct);
    }

    private async Task<Result<DeviceResponse>> ReplaceDeviceAsync(
        Device original,
        DeviceReplaceRequest request,
        CancellationToken ct
    )
    {
        var substituteResult = await FindSubstitute(request, ct);

        if (substituteResult.IsFailure)
        {
            return substituteResult.Error;
        }

        var substitute = substituteResult.Value;

        if (substitute.DeviceId == original.DeviceId)
        {
            return DeviceErrors.SubstituteTheSame();
        }

        if (!original.EqualsWithCapabilities(substitute))
        {
            return DeviceErrors.SubstituteMismatch(substitute.FriendlyName);
        }

        if (substitute.DeviceStatus != DeviceStatus.PENDING)
        {
            return DeviceErrors.SubstituteNotPending(substitute.FriendlyName);
        }

        // try to disconnect the original device,
        // We don't return on failure, because maybe the device with chirpstack integration
        // was corrupted somehow
        var disconnectResult = await _chirpstackService.DisconnectAsync(
            original.DevEui,
            CancellationToken.None
        );
        if (disconnectResult.IsFailure)
        {
            _logger.DownlinkFailed(original.DevEui, disconnectResult.Error.Description);
        }

        var acceptResult = await _chirpstackService.AcceptAsync(substitute.DevEui, ct);
        if (acceptResult.IsFailure)
        {
            _logger.DownlinkFailed(substitute.DevEui, acceptResult.Error.Description);
            return acceptResult.Error;
        }

        switch (request.ReadingsBehaviour)
        {
            // this is default behaviour
            case DataBehaviour.DISCARD:
                break;
            case DataBehaviour.OVERLAP:
                throw new NotImplementedException();
            default:
                break;
        }

        await ExecuteDeleteAsync(substitute);

        original.DevEui = substitute.DevEui;
        original.DeviceStatus = DeviceStatus.ACTIVE;
        await _ctx.SaveChangesAsync(CancellationToken.None);
        _stateCache[substitute.DevEui] = original;

        return DeviceResponse.FromModel(original);
    }

    private async Task ExecuteDeleteAsync(Device substitute)
    {
        // wipe out readings
        foreach (var sensor in substitute.Sensors)
        {
            await _ctx
                .SensorReadings.Where(r => r.SensorId == sensor.SensorId)
                .ExecuteDeleteAsync(CancellationToken.None);

            _logger.ReadingsDeleted(sensor.SensorId);
        }

        // wipe out substitute sensors
        await _ctx
            .Sensors.Where(s => s.DeviceId == substitute.DeviceId)
            .ExecuteDeleteAsync(CancellationToken.None);

        // wipe out substitute functions
        await _ctx
            .Functions.Where(f => f.DeviceId == substitute.DeviceId)
            .ExecuteDeleteAsync(CancellationToken.None);

        // wipe device
        await _ctx
            .Devices.Where(d => d.DeviceId == substitute.DeviceId)
            .ExecuteDeleteAsync(CancellationToken.None);

        _logger.DeviceDeleted(substitute.FriendlyName, substitute.DeviceId);
    }

    private async Task<Result<Device>> FindSubstitute(
        DeviceReplaceRequest request,
        CancellationToken ct
    )
    {
        async Task<Result<Device>> FindHelper(
            Expression<Func<Device, bool>> predicate,
            Func<Error> notFound,
            CancellationToken ct
        )
        {
            var device = await _ctx
                .Devices.Include(d => d.Functions)
                .Include(d => d.Sensors)
                .FirstOrDefaultAsync(predicate, ct);
            return device is null ? notFound() : device;
        }

        return request.With switch
        {
            Guid deviceId => await FindHelper(
                d => d.DeviceId == deviceId,
                () => DeviceErrors.SubstituteNotFound(deviceId),
                ct
            ),
            DevEui devEui => await FindHelper(
                d => d.DevEui == devEui,
                () => DeviceErrors.SubstituteNotFound(devEui),
                ct
            ),
            string friendlyName => await FindHelper(
                d => d.FriendlyName == friendlyName,
                () => DeviceErrors.SubstituteNotFound(friendlyName),
                ct
            ),
            _ => DeviceErrors.InvalidSubstituteKey(request.With),
        };
    }
}
