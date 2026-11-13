using Chirpstack.Api;
using Google.Protobuf;
using Grpc.Core;
using Loris.Protocol;
using Loris.Types;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class ExampleDownlinkController(
    DeviceService.DeviceServiceClient client,
    [FromKeyedServices("chirpstack-api-token")] Metadata authToken
) : ControllerBase
{
    private readonly DeviceService.DeviceServiceClient _client = client;
    private readonly JsonParser _protoParser = JsonParser.Default;
    private readonly Metadata _authToken = authToken;

    [HttpPost("{devEui}")]
    public async Task<ActionResult<string>> Hello(DevEui devEui)
    {
        var message = new Downlink { Ping = new DownPing { Type = DownPingType.Hello } };
        return await RequestDownlink(devEui, message);
    }

    [HttpPost("{devEui}")]
    public async Task<ActionResult<string>> Disconnect(DevEui devEui)
    {
        var message = new Downlink { Ping = new DownPing { Type = DownPingType.Disconnect } };
        return await RequestDownlink(devEui, message);
    }

    [HttpPost("{devEui}")]
    public async Task<ActionResult<string>> Call(DevEui devEui)
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();

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

        return await RequestDownlink(devEui, message);
    }

    private async Task<ActionResult<string>> RequestDownlink(DevEui devEui, Downlink message)
    {
        var req = new EnqueueDeviceQueueItemRequest()
        {
            QueueItem = new DeviceQueueItem()
            {
                Confirmed = false,
                Data = message.ToByteString(),
                DevEui = devEui.ToString(),
                FPort = 10, // TODO: ask team for this
            },
        };

        EnqueueDeviceQueueItemResponse resp;
        try
        {
            resp = await _client.EnqueueAsync(req, headers: _authToken);
        }
        catch
        {
            return NotFound();
        }

        return Ok(resp.Id);
    }
}
