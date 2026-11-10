using Chirpstack.Api;
using Google.Protobuf;
using Grpc.Core;
using Loris.Protocol;
using Loris.Types;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public class ExampleDownlinkController(DeviceService.DeviceServiceClient client) : ControllerBase
{
    private readonly DeviceService.DeviceServiceClient _client = client;
    private readonly JsonParser _protoParser = JsonParser.Default;

    public async Task<IActionResult> Test()
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

        var apiToken =
            "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJhdWQiOiJjaGlycHN0YWNrIiwiaXNzIjoiY2hpcnBzdGFjayIsInN1YiI6ImNkMGI2ZmZlLTg1ZWItNDUyNC1iNTdmLTJhYzA1NjM5NTgyMyIsInR5cCI6ImtleSJ9.JC-EzdivDjKX3nyafEyOnLLd_jP37-dPUTfoUn15UT8";

        var authToken = new Metadata { { "Authorization", "Bearer " + apiToken } };

        var req = new EnqueueDeviceQueueItemRequest()
        {
            QueueItem = new DeviceQueueItem()
            {
                Confirmed = false,
                Data = message.ToByteString(),
                DevEui = DevEui.Parse("11:8f:3d:af:58:5b".Replace(":", "")).ToString(),
                FPort = 10,
            },
        };

        var resp = await _client.EnqueueAsync(req, headers: authToken);

        return Ok();
    }
}
