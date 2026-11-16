using System.ComponentModel;
using Loris.Extensions;
using Loris.Models;
using Loris.Results;
using Loris.Services;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType<Device>(StatusCodes.Status200OK, Description = "Success")]
[ProducesResponseType<ResultProblemDetails>(
    StatusCodes.Status404NotFound,
    Description = "The device was not found"
)]
[ProducesResponseType<ResultProblemDetails>(
    StatusCodes.Status409Conflict,
    Description = "The device is in a state that does not allow the requested operation"
)]
public class DevicesController(IDeviceService deviceService) : ControllerBase
{
    private readonly IDeviceService _service = deviceService;

    private const string BadGatewayDescription =
        "The LoraWAN server returned an error when trying to perform the requested operation";
    private const string GatewayTimeoutDescription =
        "The LoraWAN server did not respond in time when trying to perform the requested operation";

    private const string AcceptDescription =
        @"Accepts a device, which will force a device to send reports
        instead of capabilities. It will mark a device as accepted
        and it will be able to send reports until it is disconnected.";

    private const string DisconnectDescription =
        @"Disconnects a device, which will force a device to send capabilities
        instead of reports. It will mark a device as disconnected,
        and it will not be able to send reports until it is accepted again.";

    private const string IgnoreDescription =
        @"Ignores a device, which will mark the device as ignored in the application.
        The operation has no effect on the device itself, and it will continue to send capabilities";

    // ACCEPT AMIGOS

    [HttpPost("by-name/{name}/accept")]
    [ProducesResponseType<ResultProblemDetails>(
        StatusCodes.Status502BadGateway,
        Description = BadGatewayDescription
    )]
    [ProducesResponseType<ResultProblemDetails>(
        StatusCodes.Status504GatewayTimeout,
        Description = GatewayTimeoutDescription
    )]
    [EndpointDescription(AcceptDescription)]
    public async Task<ActionResult<Device>> AcceptByName(string name, CancellationToken ct) =>
        this.ToActionResult(await _service.AcceptAsync(name, ct));

    [HttpPost("by-eui/{devEui}/accept")]
    [ProducesResponseType<ResultProblemDetails>(
        StatusCodes.Status502BadGateway,
        Description = BadGatewayDescription
    )]
    [ProducesResponseType<ResultProblemDetails>(
        StatusCodes.Status504GatewayTimeout,
        Description = GatewayTimeoutDescription
    )]
    [EndpointDescription(AcceptDescription)]
    public async Task<ActionResult<Device>> AcceptByEui(DevEui devEui, CancellationToken ct) =>
        this.ToActionResult(await _service.AcceptAsync(devEui, ct));

    // IGNORE AMIGOS

    [HttpPost("by-name/{name}/ignore")]
    [EndpointDescription(IgnoreDescription)]
    public async Task<ActionResult<Device>> IgnoreByName(
        string name,
        [DefaultValue(false)] [FromQuery] bool revert,
        CancellationToken ct
    ) => this.ToActionResult(await _service.ToggleIgnoreAsync(name, ignore: !revert, ct));

    [HttpPost("by-eui/{devEui}/ignore")]
    [EndpointDescription(IgnoreDescription)]
    public async Task<ActionResult<Device>> IgnoreByEui(
        DevEui devEui,
        [DefaultValue(false)] [FromQuery] bool revert,
        CancellationToken ct
    ) => this.ToActionResult(await _service.ToggleIgnoreAsync(devEui, ignore: !revert, ct));

    // DISCONNECT AMIGOS

    [HttpPost("by-name/{name}/disconnect")]
    [ProducesResponseType<ResultProblemDetails>(
        StatusCodes.Status502BadGateway,
        Description = BadGatewayDescription
    )]
    [ProducesResponseType<ResultProblemDetails>(
        StatusCodes.Status504GatewayTimeout,
        Description = GatewayTimeoutDescription
    )]
    [EndpointDescription(DisconnectDescription)]
    public async Task<ActionResult<Device>> DisconnectByName(
        string name,
        [DefaultValue(false)] [FromQuery] bool force,
        CancellationToken ct
    ) => this.ToActionResult(await _service.DisconnectAsync(name, force, ct));

    [HttpPost("by-eui/{devEui}/disconnect")]
    [ProducesResponseType<ResultProblemDetails>(
        StatusCodes.Status502BadGateway,
        Description = "The LoraWAN server returned an error when trying to disconnect the device"
    )]
    [ProducesResponseType<ResultProblemDetails>(
        StatusCodes.Status504GatewayTimeout,
        Description = "The LoraWAN server did not respond in time when trying to disconnect the device"
    )]
    [EndpointDescription(DisconnectDescription)]
    public async Task<ActionResult<Device>> DisconnectByEui(
        DevEui devEui,
        [DefaultValue(false)] [FromQuery] bool force,
        CancellationToken ct
    ) => this.ToActionResult(await _service.DisconnectAsync(devEui, force, ct));
}
