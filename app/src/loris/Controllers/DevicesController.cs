using System.ComponentModel;
using Loris.Dtos.Device;
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
    /// Fetches all devices with sensors and functions attached.
    /// </remarks>
    /// <param name="status">
    /// Optionally, filter by device status. Accepts unions.
    /// </param>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<DeviceDetailsResponse>>> GetAll(
        [DefaultValue(null)] [FromQuery] List<DeviceStatus> status,
        CancellationToken ct
    ) => this.ToActionResult(await _service.ListDevicesAsync(status, ct));

    /// <summary>
    /// Get device by id
    /// </summary>
    /// <remarks>
    /// Fetches a device identified by its unique ID
    /// with definitions for sensors and functions attached.
    /// </remarks>
    [HttpGet("by-id/{id}")]
    [ProducesDeviceNotFound]
    public async Task<ActionResult<DeviceDetailsResponse>> GetById(Guid id, CancellationToken ct) =>
        this.ToActionResult(await _service.GetAsync(id, ct));

    /// <summary>
    /// Get device by devEui
    /// </summary>
    /// <remarks>
    /// Fetches a device identified by its unique LoraWAN devEUI address
    /// with definitions for sensors and functions attached.
    /// </remarks>
    [HttpGet("by-eui/{devEui}")]
    [ProducesDeviceNotFound]
    public async Task<ActionResult<DeviceDetailsResponse>> GetByEui(
        DevEui devEui,
        CancellationToken ct
    ) => this.ToActionResult(await _service.GetAsync(devEui, ct));

    /// <summary>
    /// Get device by name
    /// </summary>
    /// <remarks>
    /// Fetches a device identified by its unique friendly name
    /// with definitions for sensors and functions attached.
    /// </remarks>
    [HttpGet("by-name/{name}")]
    [ProducesDeviceNotFound]
    public async Task<ActionResult<DeviceDetailsResponse>> GetByName(
        string name,
        CancellationToken ct
    ) => this.ToActionResult(await _service.GetAsync(name, ct));

    /// <summary>
    /// Rename device by id
    /// </summary>
    /// <remarks>
    /// Renames a device identified by its unique ID.
    /// Returns the updated device only.
    /// </remarks>
    [HttpPatch("by-id/{id}")]
    [ProducesDeviceNameConflict]
    [ProducesDeviceNotFound]
    public async Task<ActionResult<DeviceResponse>> RenameById(
        Guid id,
        [FromBody] RenameDeviceRequest request,
        CancellationToken ct
    ) => this.ToActionResult(await _service.RenameAsync(id, request.NewFriendlyName, ct));

    /// <summary>
    /// Rename device by devEui
    /// </summary>
    /// <remarks>
    /// Renames a device identified by its unique LoraWAN devEUI address.
    /// Returns the updated device only.
    /// </remarks>
    [HttpPatch("by-eui/{devEui}")]
    [ProducesDeviceNameConflict]
    [ProducesDeviceNotFound]
    public async Task<ActionResult<DeviceResponse>> RenameByEui(
        DevEui devEui,
        [FromBody] RenameDeviceRequest request,
        CancellationToken ct
    ) => this.ToActionResult(await _service.RenameAsync(devEui, request.NewFriendlyName, ct));

    /// <summary>
    /// Rename device by name
    /// </summary>
    /// <remarks>
    /// Renames a device identified by its unique friendly name.
    /// Returns the updated device only.
    /// </remarks>
    [HttpPatch("by-name/{name}")]
    [ProducesDeviceNameConflict]
    [ProducesDeviceNotFound]
    public async Task<ActionResult<DeviceResponse>> RenameByName(
        string name,
        [FromBody] RenameDeviceRequest request,
        CancellationToken ct
    ) => this.ToActionResult(await _service.RenameAsync(name, request.NewFriendlyName, ct));

    /// <summary>
    /// Accept device by id
    /// </summary>
    /// <remarks>
    /// Accepts a device, forcing it to send reports instead of capabilities until disconnected.
    /// </remarks>
    [HttpPost("by-id/{id}/accept")]
    [ProducesLoraWanGatewayErrors]
    [ProducesDeviceStateConflict]
    [ProducesDeviceNotFound]
    public async Task<ActionResult<DeviceResponse>> AcceptById(Guid id, CancellationToken ct) =>
        this.ToActionResult(await _service.AcceptAsync(id, ct));

    /// <summary>
    /// Accept device by name
    /// </summary>
    /// <remarks>
    /// Accepts a device, forcing it to send reports instead of capabilities until disconnected.
    /// </remarks>
    [HttpPost("by-name/{name}/accept")]
    [ProducesLoraWanGatewayErrors]
    [ProducesDeviceStateConflict]
    [ProducesDeviceNotFound]
    public async Task<ActionResult<DeviceResponse>> AcceptByName(
        string name,
        CancellationToken ct
    ) => this.ToActionResult(await _service.AcceptAsync(name, ct));

    /// <summary>
    /// Accept device by devEui
    /// </summary>
    /// <remarks>
    /// Accepts a device, forcing it to send reports instead of capabilities until disconnected.
    /// </remarks>
    [HttpPost("by-eui/{devEui}/accept")]
    [ProducesLoraWanGatewayErrors]
    [ProducesDeviceStateConflict]
    [ProducesDeviceNotFound]
    public async Task<ActionResult<DeviceResponse>> AcceptByEui(
        DevEui devEui,
        CancellationToken ct
    ) => this.ToActionResult(await _service.AcceptAsync(devEui, ct));

    /// <summary>
    /// Ignore device by id
    /// </summary>
    /// <remarks>
    /// Marks the device as ignored in the application without affecting the device itself.
    /// </remarks>
    /// <param name="revert">
    /// If true, the device will be un-ignored.
    /// </param>
    [HttpPost("by-id/{id}/ignore")]
    [ProducesDeviceStateConflict]
    [ProducesDeviceNotFound]
    public async Task<ActionResult<DeviceResponse>> IgnoreById(
        Guid id,
        [DefaultValue(false)] [FromQuery] bool revert,
        CancellationToken ct
    ) => this.ToActionResult(await _service.ToggleIgnoreAsync(id, ignore: !revert, ct));

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
    [ProducesDeviceStateConflict]
    [ProducesDeviceNotFound]
    public async Task<ActionResult<DeviceResponse>> IgnoreByName(
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
    [ProducesDeviceStateConflict]
    [ProducesDeviceNotFound]
    public async Task<ActionResult<DeviceResponse>> IgnoreByEui(
        DevEui devEui,
        [DefaultValue(false)] [FromQuery] bool revert,
        CancellationToken ct
    ) => this.ToActionResult(await _service.ToggleIgnoreAsync(devEui, ignore: !revert, ct));

    /// <summary>
    /// Disconnect device by id
    /// </summary>
    /// <remarks>
    /// Disconnects a device, forcing it to send capabilities instead of reports until the next uplink.
    /// </remarks>
    /// <param name="force">
    /// If true, the device will be disconnected even if it is not accepted.
    /// </param>
    [HttpPost("by-id/{id}/disconnect")]
    [ProducesLoraWanGatewayErrors]
    [ProducesDeviceStateConflict]
    [ProducesDeviceNotFound]
    public async Task<ActionResult<DeviceResponse>> DisconnectByName(
        Guid id,
        [DefaultValue(false)] [FromQuery] bool force,
        CancellationToken ct
    ) => this.ToActionResult(await _service.DisconnectAsync(id, force, ct));

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
    [ProducesDeviceStateConflict]
    [ProducesDeviceNotFound]
    public async Task<ActionResult<DeviceResponse>> DisconnectByName(
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
    [ProducesDeviceStateConflict]
    [ProducesDeviceNotFound]
    public async Task<ActionResult<DeviceResponse>> DisconnectByEui(
        DevEui devEui,
        [DefaultValue(false)] [FromQuery] bool force,
        CancellationToken ct
    ) => this.ToActionResult(await _service.DisconnectAsync(devEui, force, ct));
}
