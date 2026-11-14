using Chirpstack.Api;
using Grpc.Core;
using Loris.Data;
using Loris.Extensions;
using Loris.Protocol;
using Loris.Types;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Device = Loris.Models.Device;
using DeviceStatus = Loris.Models.DeviceStatus;

namespace Loris.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DevicesController(
    ILogger<DevicesController> logger,
    LorisDbContext ctx,
    IStateCache stateCache,
    DeviceService.DeviceServiceClient client,
    [FromKeyedServices("chirpstack-api-token")] Metadata authToken
) : ControllerBase
{
    private readonly ILogger<DevicesController> _logger = logger;
    private readonly LorisDbContext _ctx = ctx;
    private readonly IStateCache _stateCache = stateCache;
    private readonly DeviceService.DeviceServiceClient _client = client;
    private readonly Metadata _authToken = authToken;

    [Route("by-name/{name}/[action]")]
    [HttpPost()]
    public async Task<ActionResult<Device>> Accept(string name, CancellationToken ct)
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.FriendlyName == name, ct);

        if (device is null)
        {
            return NotFound();
        }

        return await HandleAcceptAsync(device, ct);
    }

    [Route("by-eui/{devEui}/[action]")]
    [HttpPost()]
    public async Task<ActionResult<Device>> Accept(DevEui devEui, CancellationToken ct)
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.DevEui == devEui, ct);

        if (device is null)
        {
            return NotFound();
        }

        return await HandleAcceptAsync(device, ct);
    }

    [Route("by-name/{name}/[action]")]
    [HttpPost()]
    public async Task<ActionResult<Device>> Ignore(
        string name,
        [FromQuery] bool revert,
        CancellationToken ct
    )
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.FriendlyName == name, ct);

        if (device is null)
        {
            return NotFound();
        }

        return await HandleIgnoreAsync(device, revert, ct);
    }

    [Route("by-eui/{devEui}/[action]")]
    [HttpPost()]
    public async Task<ActionResult<Device>> Ignore(
        DevEui devEui,
        [FromQuery] bool revert,
        CancellationToken ct
    )
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.DevEui == devEui, ct);

        if (device is null)
        {
            return NotFound();
        }

        return await HandleIgnoreAsync(device, revert, ct);
    }

    [Route("by-eui/{devEui}/[action]")]
    [HttpPost()]
    public async Task<ActionResult<Device>> Disconnect(
        DevEui devEui,
        CancellationToken ct,
        [FromQuery] bool force = false
    )
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.DevEui == devEui, ct);
        if (device is null)
        {
            return NotFound();
        }

        return await HandleDisconnectAsync(device, ct);
    }

    [Route("by-name/{name}/[action]")]
    [HttpPost()]
    public async Task<ActionResult<Device>> Disconnect(string name, CancellationToken ct)
    {
        var device = await _ctx.Devices.SingleOrDefaultAsync(d => d.FriendlyName == name, ct);
        if (device is null)
        {
            return NotFound();
        }
        return await HandleDisconnectAsync(device, ct);
    }

    private async Task<ActionResult<Device>> HandleDisconnectAsync(
        Device device,
        CancellationToken ct
    )
    {
        return device.DeviceStatus switch
        {
            DeviceStatus.ACTIVE => await DisconnectAsync(device, ct),
            DeviceStatus.PENDING => Conflict("Device is not yet connected"),
            DeviceStatus.BLOCKED => Conflict("Device is not yet connected"),
            DeviceStatus.DISCONNECTED => Conflict("Device already disconnected"),
            _ => Conflict("Device status is unknown"),
        };
    }

    private async Task<ActionResult<Device>> DisconnectAsync(Device device, CancellationToken ct)
    {
        throw new NotImplementedException();
        // device.DeviceStatus = DeviceStatus.DISCONNECTED;
        // var downlink = new Downlink() { Ping = new DownPing { Type = DownPingType.Disconnect } };
        //
        // var downlinkTask = _client.EnqueueDownlinkAsync(downlink, device.DevEui, _authToken, ct);
        // var dbTask = _ctx.SaveChangesAsync(ct);
        //
        // // State cache remove from ACCEPTED
        // _stateCache.UpdateDevices(device.DevEui, accepted: false);
        //
        // return Ok();
    }

    private async Task<ActionResult<Device>> HandleIgnoreAsync(
        Device device,
        bool revert,
        CancellationToken ct
    )
    {
        return (device.DeviceStatus, revert) switch
        {
            (DeviceStatus.BLOCKED, true) => await ToggleIgnoreDeviceAsync(
                device,
                DeviceStatus.PENDING,
                ct
            ),
            (DeviceStatus.PENDING, false) => await ToggleIgnoreDeviceAsync(
                device,
                DeviceStatus.BLOCKED,
                ct
            ),
            (DeviceStatus.PENDING, true) => Conflict("Device is pending"),
            (DeviceStatus.BLOCKED, false) => Conflict("Device is alredy ignored"),
            (DeviceStatus.ACTIVE, _) => Conflict("Device is already accepted"),
            (DeviceStatus.DISCONNECTED, _) => Conflict("Device disconnected"),
            _ => Conflict("Device status is unknown"),
        };
    }

    private async Task<ActionResult<Device>> ToggleIgnoreDeviceAsync(
        Device device,
        DeviceStatus status,
        CancellationToken ct
    )
    {
        device.DeviceStatus = status;
        await _ctx.SaveChangesAsync(ct);
        return Ok(device);
    }

    private async Task<ActionResult<Device>> HandleAcceptAsync(Device device, CancellationToken ct)
    {
        return device.DeviceStatus switch
        {
            DeviceStatus.PENDING => await AcceptDeviceAsync(device, ct),
            DeviceStatus.ACTIVE => Conflict("Device is already active"),
            DeviceStatus.BLOCKED => Conflict("Device is blocked"),
            DeviceStatus.DISCONNECTED => Conflict("Device is disconnected"),
            _ => Conflict("Device status is unknown"),
        };
    }

    private async Task<ActionResult<Device>> AcceptDeviceAsync(Device device, CancellationToken ct)
    {
        device.DeviceStatus = DeviceStatus.ACTIVE;
        _stateCache.UpdateDevices(device.DevEui);
        await _ctx.SaveChangesAsync(ct);
        return Ok(device);
    }
}
