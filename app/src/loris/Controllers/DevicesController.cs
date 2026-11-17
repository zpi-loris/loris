using System.ComponentModel;
using Loris.Extensions;
using Loris.Models;
using Loris.OpenApi;
using Loris.Services;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesUnauthenticated]
[ProducesSuccess]
public class DevicesController(IDeviceService deviceService) : ControllerBase
{
    protected readonly IDeviceService _service = deviceService;

    /// <summary>
    /// Get all devices
    /// </summary>
    /// <remarks>
    /// Get all devices with sensors and functions.
    /// </remarks>
    /// <param name="status">
    /// Optionally, filter by device status. Accepts unions.
    /// </param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Device>>> GetAll(
        [DefaultValue(null)] [FromQuery] DeviceStatus[] status,
        CancellationToken ct
    ) => throw new NotImplementedException();

    /// <summary>
    /// Get device by id
    /// </summary>
    /// <remarks>
    /// Get a device with sensors and functions by its id.
    /// </remarks>
    [HttpGet("by-id/{id}")]
    [ProducesSensorNotFound]
    public async Task<ActionResult<Device>> GetById(Guid id, CancellationToken ct) =>
        throw new NotImplementedException();

    /// <summary>
    /// Get device by devEui
    /// </summary>
    /// <remarks>
    /// Get a device with sensors and functions by its devEUI.
    /// </remarks>
    [HttpGet("by-eui/{devEui}")]
    [ProducesSensorNotFound]
    public async Task<ActionResult<Device>> GetByEui(DevEui devEui, CancellationToken ct) =>
        throw new NotImplementedException();

    /// <summary>
    /// Get device by name
    /// </summary>
    /// <remarks>
    /// Get a device with sensors and functions by its name.
    /// </remarks>
    [HttpGet("by-name/{name}")]
    [ProducesSensorNotFound]
    public async Task<ActionResult<Device>> GetByName(string name, CancellationToken ct) =>
        throw new NotImplementedException();

    /// <summary>
    /// Accept device by name
    /// </summary>
    /// <remarks>
    /// Accepts a device, forcing it to send reports instead of capabilities until disconnected.
    /// </remarks>
    [HttpPost("by-name/{name}/accept")]
    [ProducesLoraWanGatewayErrors]
    [ProducesDeviceConflict]
    [ProducesSensorNotFound]
    public async Task<ActionResult<Device>> AcceptByName(string name, CancellationToken ct) =>
        this.ToActionResult(await _service.AcceptAsync(name, ct));

    /// <summary>
    /// Accept device by devEui
    /// </summary>
    /// <remarks>
    /// Accepts a device, forcing it to send reports instead of capabilities until disconnected.
    /// </remarks>
    [HttpPost("by-eui/{devEui}/accept")]
    [ProducesLoraWanGatewayErrors]
    [ProducesDeviceConflict]
    [ProducesSensorNotFound]
    public async Task<ActionResult<Device>> AcceptByEui(DevEui devEui, CancellationToken ct) =>
        this.ToActionResult(await _service.AcceptAsync(devEui, ct));

    /// <summary>
    /// Ignore device by name
    /// </summary>
    /// <remarks>
    /// Marks the device as ignored in the application without affecting the device itself.
    /// </remarks>
    /// <param name="revert">
    /// If true, the device will be un-ignored.
    /// </param>
    [HttpPost("by-name/{name}/ignore")]
    [ProducesDeviceConflict]
    [ProducesSensorNotFound]
    public async Task<ActionResult<Device>> IgnoreByName(
        string name,
        [DefaultValue(false)] [FromQuery] bool revert,
        CancellationToken ct
    ) => this.ToActionResult(await _service.ToggleIgnoreAsync(name, ignore: !revert, ct));

    /// <summary>
    /// Ignore device by devEui
    /// </summary>
    /// <remarks>
    /// Marks the device as ignored in the application without affecting the device itself.
    /// </remarks>
    /// <param name="revert">
    /// If true, the device will be un-ignored.
    /// </param>
    [HttpPost("by-eui/{devEui}/ignore")]
    [ProducesDeviceConflict]
    [ProducesSensorNotFound]
    public async Task<ActionResult<Device>> IgnoreByEui(
        DevEui devEui,
        [DefaultValue(false)] [FromQuery] bool revert,
        CancellationToken ct
    ) => this.ToActionResult(await _service.ToggleIgnoreAsync(devEui, ignore: !revert, ct));

    /// <summary>
    /// Disconnect device by name
    /// </summary>
    /// <remarks>
    /// Disconnects a device, forcing it to send capabilities instead of reports until the next uplink.
    /// </remarks>
    /// <param name="force">
    /// If true, the device will be disconnected even if it is not accepted.
    /// </param>
    [HttpPost("by-name/{name}/disconnect")]
    [ProducesLoraWanGatewayErrors]
    [ProducesDeviceConflict]
    [ProducesSensorNotFound]
    public async Task<ActionResult<Device>> DisconnectByName(
        string name,
        [DefaultValue(false)] [FromQuery] bool force,
        CancellationToken ct
    ) => this.ToActionResult(await _service.DisconnectAsync(name, force, ct));

    /// <summary>
    /// Disconnect device by devEui
    /// </summary>
    /// <remarks>
    /// Disconnects a device, forcing it to send capabilities instead of reports until the next uplink.
    /// </remarks>
    /// <param name="force">
    /// If true, the device will be disconnected even if it is not accepted.
    /// </param>
    [HttpPost("by-eui/{devEui}/disconnect")]
    [ProducesLoraWanGatewayErrors]
    [ProducesDeviceConflict]
    [ProducesSensorNotFound]
    public async Task<ActionResult<Device>> DisconnectByEui(
        DevEui devEui,
        [DefaultValue(false)] [FromQuery] bool force,
        CancellationToken ct
    ) => this.ToActionResult(await _service.DisconnectAsync(devEui, force, ct));
}
