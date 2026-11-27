using Loris.Models;
using Loris.Protocol;
using Loris.Results;
using loris.Services;

namespace loris.IntegrationTests.Fakes;

public class FakeChirpstackServiceWithSuccess : IChirpstackService
{
    public async Task<Result> AcceptAsync(DevEui devEui, CancellationToken ct = default) =>
        Result.Success();

    public async Task<Result> AcceptAsync(Guid deviceId, CancellationToken ct = default) =>
        Result.Success();

    public async Task<Result> CallFunctionAsync(
        DevEui devEui,
        Call message,
        CancellationToken ct = default
    ) => Result.Success();

    public async Task<Result> CallFunctionAsync(
        Guid deviceId,
        Call message,
        CancellationToken ct = default
    ) => Result.Success();

    public async Task<Result> DisconnectAsync(DevEui devEui, CancellationToken ct = default) =>
        Result.Success();

    public async Task<Result> DisconnectAsync(Guid deviceId, CancellationToken ct = default) =>
        Result.Success();
}
