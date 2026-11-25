using Chirpstack.Api;
using Loris.Data;
using Loris.Extensions;
using Loris.Logging;
using Loris.Models;
using Loris.Protocol;
using Loris.Results;

namespace loris.Services;

public class ChirpstackService(
    ILogger<ChirpstackService> logger,
    IStateCache stateCache,
    DeviceService.DeviceServiceClient downlinkClient
) : IChirpstackService
{
    // TODO: consider logging
    private readonly ILogger<ChirpstackService> _logger = logger;
    private readonly IStateCache _stateCache = stateCache;
    private readonly DeviceService.DeviceServiceClient _downlinkClient = downlinkClient;

    public Task<Result> AcceptAsync(DevEui devEui, CancellationToken ct = default)
    {
        var downlink = new Downlink { Ping = new DownPing { Type = DownPingType.Hello } };
        return _downlinkClient.EnqueueDownlinkAsync(downlink, devEui, ct);
    }

    public Task<Result> AcceptAsync(Guid deviceId, CancellationToken ct = default)
    {
        // TODO: consider what if cache miss
        var devEui = _stateCache[deviceId];
        return AcceptAsync(devEui, ct);
    }

    public Task<Result> CallFunctionAsync(DevEui devEui, Call call, CancellationToken ct = default)
    {
        var downlink = new Downlink { Call = call };
        return _downlinkClient.EnqueueDownlinkAsync(downlink, devEui, ct);
    }

    public Task<Result> CallFunctionAsync(Guid deviceId, Call call, CancellationToken ct = default)
    {
        // TODO: consider what if cache miss
        var devEui = _stateCache[deviceId];
        return CallFunctionAsync(devEui, call, ct);
    }

    public Task<Result> DisconnectAsync(DevEui devEui, CancellationToken ct = default)
    {
        var downlink = new Downlink { Ping = new DownPing { Type = DownPingType.Disconnect } };
        return _downlinkClient.EnqueueDownlinkAsync(downlink, devEui, ct);
    }

    public Task<Result> DisconnectAsync(Guid deviceId, CancellationToken ct = default)
    {
        // TODO: consider what if cache miss
        var devEui = _stateCache[deviceId];
        return DisconnectAsync(devEui, ct);
    }
}
