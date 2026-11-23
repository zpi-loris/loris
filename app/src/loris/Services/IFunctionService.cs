using Loris.Models;
using Loris.Results;

namespace Loris.Services;

public interface IFunctionService
{
    public Task<Result> CallFunction(Guid id, object? arg, CancellationToken ct = default);
    public Task<Result> CallFunction(
        DevEui devEui,
        int deviceFunctionId,
        object? arg,
        CancellationToken ct = default
    );
}
