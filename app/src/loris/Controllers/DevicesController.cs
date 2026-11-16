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

    // ACCEPT AMIGOS

    [HttpPost("by-name/{name}/accept")]
    [ProducesResponseType<ResultProblemDetails>(
        StatusCodes.Status502BadGateway,
        Description = "The LoraWAN server returned an error when trying to accept the device"
    )]
    [ProducesResponseType<ResultProblemDetails>(
        StatusCodes.Status504GatewayTimeout,
        Description = "The LoraWAN server did not respond in time when trying to accept the device"
    )]
    public async Task<ActionResult<Device>> AcceptByName(string name, CancellationToken ct) =>
        this.ToActionResult(await _service.AcceptAsync(name, ct));

    [HttpPost("by-eui/{devEui}/accept")]
    [ProducesResponseType<ResultProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<Device>> AcceptByEui(DevEui devEui, CancellationToken ct) =>
        this.ToActionResult(await _service.AcceptAsync(devEui, ct));

    // IGNORE AMIGOS

    [HttpPost("by-name/{name}/ignore")]
    public async Task<ActionResult<Device>> IgnoreByName(
        string name,
        [DefaultValue(false)] [FromQuery] bool revert,
        CancellationToken ct
    ) => this.ToActionResult(await _service.ToggleIgnoreAsync(name, ignore: !revert, ct));

    [HttpPost("by-eui/{devEui}/ignore")]
    public async Task<ActionResult<Device>> IgnoreByEui(
        DevEui devEui,
        [DefaultValue(false)] [FromQuery] bool revert,
        CancellationToken ct
    ) => this.ToActionResult(await _service.ToggleIgnoreAsync(devEui, ignore: !revert, ct));

    // DISCONNECT AMIGOS

    [HttpPost("by-name/{name}/disconnect")]
    [ProducesResponseType<ResultProblemDetails>(StatusCodes.Status502BadGateway)]
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
    public async Task<ActionResult<Device>> DisconnectByEui(
        DevEui devEui,
        [DefaultValue(false)] [FromQuery] bool force,
        CancellationToken ct
    ) => this.ToActionResult(await _service.DisconnectAsync(devEui, force, ct));
}
