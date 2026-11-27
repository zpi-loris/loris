using System.Net.Sockets;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class TestingController(ILogger<TestingController> logger) : ControllerBase
{
    private readonly ILogger<TestingController> _logger = logger;

    [HttpGet]
    public void Test()
    {
        _logger.LogInformation(
            "LocalIpAddress: {ip}",
            Request.HttpContext.Connection.LocalIpAddress
        );
        _logger.LogInformation("LocalPort : {port}", Request.HttpContext.Connection.LocalPort);
        _logger.LogInformation("The host is: {Host}", Request.Host);
        _logger.LogInformation("Path: {Path}", Request.Path);
        _logger.LogInformation("PathBase: {PathBase}", Request.PathBase);
        object? socketPath = null;
        HttpContext
            .Features.Get<IConnectionItemsFeature>()
            ?.Items.TryGetValue("UnixSocketPath", out socketPath);
        _logger.LogInformation("Items socket: {socket}", socketPath ?? "null");
    }
}
