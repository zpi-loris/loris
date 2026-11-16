using Chirpstack.Api;
using Google.Protobuf;
using Grpc.Core;
using Loris.Extensions;
using Loris.Models;
using Loris.Protocol;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class ExampleDownlinkController(DeviceService.DeviceServiceClient client) : ControllerBase
{
    private readonly DeviceService.DeviceServiceClient _client = client;
    private readonly JsonParser _protoParser = JsonParser.Default;

    [HttpPost("{devEui}")]
    public async Task<ActionResult<string>> Hello(DevEui devEui, CancellationToken ct)
    {
        var message = new Downlink { Ping = new DownPing { Type = DownPingType.Hello } };
        return await RequestDownlink(devEui, message, ct);
    }

    [HttpPost("{devEui}")]
    public async Task<ActionResult<string>> Disconnect(DevEui devEui, CancellationToken ct)
    {
        var message = new Downlink { Ping = new DownPing { Type = DownPingType.Disconnect } };
        return await RequestDownlink(devEui, message, ct);
    }

    [HttpPost("{devEui}")]
    public async Task<ActionResult<string>> Call(DevEui devEui, CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync(ct);

        Downlink message;
        try
        {
            message = _protoParser.Parse<Downlink>(json);
        }
        catch (InvalidJsonException)
        {
            return BadRequest("Invalid json");
        }
        catch (InvalidProtocolBufferException)
        {
            return BadRequest("Invalid protocol buffer");
        }

        if (message.PayloadCase != Downlink.PayloadOneofCase.Call)
        {
            return BadRequest();
        }

        return await RequestDownlink(devEui, message, ct);
    }

    private async Task<ActionResult<string>> RequestDownlink(
        DevEui devEui,
        Downlink message,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await _client.EnqueueDownlinkAsync(message, devEui, cancellationToken);
        }
        catch (RpcException e)
        {
            Console.WriteLine(
                $"Error while requesting downlink for device {devEui}: {e.Status.Detail}"
            );
            return NotFound();
        }

        return Ok();
    }
}
