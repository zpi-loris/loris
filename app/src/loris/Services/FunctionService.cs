using Loris.Data;
using Loris.Models;
using Loris.Protocol;
using Loris.Results;
using Loris.Results.Errors;
using loris.Services;
using Microsoft.EntityFrameworkCore;

namespace Loris.Services;

public class FunctionService(
    ILogger<FunctionService> logger,
    IChirpstackService chirpstackService,
    LorisDbContext ctx
) : IFunctionService
{
    private readonly ILogger<FunctionService> _logger = logger;
    private readonly IChirpstackService _chirpstackService = chirpstackService;
    private readonly LorisDbContext _ctx = ctx;

    public async Task<Result> CallFunction(Guid id, object? arg, CancellationToken ct = default)
    {
        var function = _ctx
            .Functions.Include(f => f.Device)
            .FirstOrDefault(f => f.FunctionId == id);

        if (function is null)
        {
            return Result.Failure(FunctionErrors.NotFound(id));
        }

        var call = new Call { FunctionId = function.DeviceFunctionId };

        switch ((function.ArgType, arg))
        {
            case (FuncArgType.Float, float floatVal):
                call.FloatVal = floatVal;
                break;
            case (FuncArgType.Int, int intVal):
                call.IntVal = intVal;
                break;
            case (FuncArgType.Bool, bool boolVal):
                call.BoolVal = boolVal;
                break;
            case (FuncArgType.String, string strVal):
                call.StrVal = strVal;
                break;
            case (FuncArgType.Float, _):
            case (FuncArgType.String, _):
            case (FuncArgType.Int, _):
            case (FuncArgType.Bool, _):
                return Result.Failure(
                    FunctionErrors.InvalidArgument(
                        function.ArgType.ToString(),
                        arg?.ToString() ?? "null"
                    )
                );
            default:
                break;
        }

        return await _chirpstackService.CallFunctionAsync(function.Device.DevEui, call, ct);
    }

    public async Task<Result> CallFunction(
        DevEui devEui,
        int functionId,
        object? arg,
        CancellationToken ct = default
    ) => throw new NotImplementedException();
}
