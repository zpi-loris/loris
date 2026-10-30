using System.Threading.Channels;
using Google.Protobuf;
using Loris.Messages;
using Loris.Protocol;
using Loris.Types;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExampleUplinkController(
    ChannelReader<IDeviceMessage> reader,
    ChannelWriter<IDeviceMessage> writer
) : ControllerBase
{
    private readonly JsonParser _protoParser = JsonParser.Default;
    private readonly JsonFormatter _protoFormatter = JsonFormatter.Default;
    private readonly ChannelReader<IDeviceMessage> _reader = reader;
    private readonly ChannelWriter<IDeviceMessage> _writer = writer;

    [HttpGet]
    public async Task<string?> GetLastPayload()
    {
        if (_reader.Count == 0)
        {
            return null;
        }
        var message = await _reader.ReadAsync();
        if (message is UplinkMessage uplink)
        {
            return _protoFormatter.Format(uplink.Payload);
        }
        else
        {
            return null;
        }
    }

    [HttpPost]
    public async Task<IActionResult> PostPayload()
    {
        using var reader = new StreamReader(Request.Body);
        var json = await reader.ReadToEndAsync();

        Uplink message;
        try
        {
            message = _protoParser.Parse<Uplink>(json);
        }
        catch (InvalidJsonException)
        {
            return BadRequest("Invalid json");
        }
        catch (InvalidProtocolBufferException)
        {
            return BadRequest("Invalid protocol buffer");
        }

        await _writer.WriteAsync(
            new UplinkMessage()
            {
                DevEUI = DevEui.Parse("11:8f:3d:af:58:5b".Replace(":", "")),
                DateTime = DateTime.Now,
                Payload = message,
            }
        );

        return Ok();
    }
}
