using System.Threading.Channels;
using app.Messages;
using app.Types;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using Protocol.V1;

namespace app.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExampleUplinkController(
    ChannelReader<IChirpstackMessage> reader,
    ChannelWriter<IChirpstackMessage> writer
) : ControllerBase
{
    private readonly JsonParser _protoParser = JsonParser.Default;
    private readonly JsonFormatter _protoFormatter = JsonFormatter.Default;
    private readonly ChannelReader<IChirpstackMessage> _reader = reader;
    private readonly ChannelWriter<IChirpstackMessage> _writer = writer;

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

        Message message;
        try
        {
            message = _protoParser.Parse<Message>(json);
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
