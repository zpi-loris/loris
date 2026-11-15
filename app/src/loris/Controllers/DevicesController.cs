using System.ComponentModel;
using Loris.Extensions;
using Loris.Models;
using Loris.Services;
using Loris.Types;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
[ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
public class DevicesController(IDeviceService deviceService) : ControllerBase
{
    private readonly IDeviceService _service = deviceService;

    // ACCEPT AMIGOS

    [HttpPost("by-name/{name}/accept")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<Device>> AcceptByName(string name, CancellationToken ct) =>
        this.ToActionResult(await _service.AcceptAsync(name, ct));

    [HttpPost("by-eui/{devEui}/accept")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
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
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<Device>> DisconnectByName(
        string name,
        [DefaultValue(false)] [FromQuery] bool force,
        CancellationToken ct
    ) => this.ToActionResult(await _service.DisconnectAsync(name, force, ct));

    [HttpPost("by-eui/{devEui}/disconnect")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<Device>> DisconnectByEui(
        DevEui devEui,
        [DefaultValue(false)] [FromQuery] bool force,
        CancellationToken ct
    ) => this.ToActionResult(await _service.DisconnectAsync(devEui, force, ct));
}
