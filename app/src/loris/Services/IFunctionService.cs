using Loris.Models;
using Loris.Results;

namespace Loris.Services;

public interface IFunctionService
{
    public Task<Result> CallFunction(Guid id);
    public Task<Result> CallFunction(DevEui devEui, int functionId);
}
