using Loris.Models;
using Loris.Protocol;
using Loris.Results;
using Loris.Results.Errors;
using loris.Services;

namespace loris.IntegrationTests.Fakes;

public class FakeChirpstackServiceWithTimeout : IChirpstackService
{
    public async Task<Result> AcceptAsync(DevEui devEui, CancellationToken ct = default) =>
        Result.Failure(DownlinkErrors.DeadlineExceeded());

    public async Task<Result> AcceptAsync(Guid deviceId, CancellationToken ct = default) =>
        Result.Failure(DownlinkErrors.DeadlineExceeded());

    public async Task<Result> CallFunctionAsync(
        DevEui devEui,
        Call message,
        CancellationToken ct = default
    ) => Result.Failure(DownlinkErrors.DeadlineExceeded());

    public async Task<Result> CallFunctionAsync(
        Guid deviceId,
        Call message,
        CancellationToken ct = default
    ) => Result.Failure(DownlinkErrors.DeadlineExceeded());

    public async Task<Result> DisconnectAsync(DevEui devEui, CancellationToken ct = default) =>
        Result.Failure(DownlinkErrors.DeadlineExceeded());

    public async Task<Result> DisconnectAsync(Guid deviceId, CancellationToken ct = default) =>
        Result.Failure(DownlinkErrors.DeadlineExceeded());
}
