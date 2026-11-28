using Loris.Data;
using Loris.Logging;
using Loris.Models;
using Loris.Protocol;
using loris.Services;
using Microsoft.EntityFrameworkCore;

namespace Loris.Automations.Actions.DeviceCall;

public class DeviceCallActionCompiler(
    IDbContextFactory<LorisDbContext> ctxFactory,
    IChirpstackService chirpstackService
) : ActionCompilerBase<DeviceCallActionDto>
{
    private readonly IDbContextFactory<LorisDbContext> _ctxFactory = ctxFactory;
    private readonly IChirpstackService _chirpstackService = chirpstackService;

    // lookups
    private const string DEVICE_FUNCTION_ID = "DeviceFunctionId";
    private const string DEVICE_ID = "DeviceId";
    private const string FUNC_ARG_TYPE = "FuncArgType";

    protected override async Task<BlockValidationResult> ValidateAsync(
        DeviceCallActionDto dto,
        CancellationToken cancellationToken
    )
    {
        using var ctx = await _ctxFactory.CreateDbContextAsync(cancellationToken);

        var device = await ctx
            .Devices.Where(d => d.DeviceStatus == DeviceStatus.ACTIVE)
            .SingleOrDefaultAsync(d => d.FriendlyName == dto.Device, cancellationToken);

        if (device is null)
        {
            return BlockValidationResult.Failure([
                new BlockError(
                    nameof(dto.Device),
                    dto.Device,
                    "There is no device with given Friendly Name"
                ),
            ]);
        }

        var function = await ctx.Functions.SingleOrDefaultAsync(
            f => f.DeviceId == device.DeviceId && f.Name == dto.Function,
            cancellationToken
        );

        if (function is null)
        {
            return BlockValidationResult.Failure([
                new BlockError(
                    nameof(dto.Function),
                    dto.Function,
                    $"[{dto.Device}] device does not have function called [{dto.Function}]"
                ),
            ]);
        }

        var isValidValue = (function.ArgType, dto.Value) switch
        {
            (FuncArgType.Float, float) => true,
            (FuncArgType.Float, int) => true,
            (FuncArgType.Int, int) => true,
            (FuncArgType.Bool, bool) => true,
            (FuncArgType.String, string) => true,
            (FuncArgType.Void, null) => true,
            (FuncArgType.Unspecified, _) => true,
            _ => false,
        };

        if (!isValidValue)
        {
            return BlockValidationResult.Failure([
                new BlockError(
                    nameof(dto.Value),
                    dto.Value?.ToString() ?? "",
                    function.ArgType == FuncArgType.Void
                        ? "Function does not accept values"
                        : $"Function accept values of type {function.ArgType}"
                ),
            ]);
        }

        var lookup = new Dictionary<string, object>
        {
            { DEVICE_ID, device.DeviceId },
            { DEVICE_FUNCTION_ID, function.DeviceFunctionId },
            { FUNC_ARG_TYPE, function.ArgType },
        };

        return BlockValidationResult.Success(lookup);
    }

    protected override Func<CancellationToken, Task> Compile(
        DeviceCallActionDto dto,
        LookupContext? lookup
    )
    {
        Call call = new() { FunctionId = lookup!.Get<int>(DEVICE_FUNCTION_ID) };

        switch (lookup!.Get<FuncArgType>(FUNC_ARG_TYPE))
        {
            case FuncArgType.Float:
                call.FloatVal = Convert.ToSingle(dto.Value);
                break;
            case FuncArgType.Int:
                call.IntVal = (int)dto.Value!;
                break;
            case FuncArgType.Bool:
                call.BoolVal = (bool)dto.Value!;
                break;
            case FuncArgType.String:
                call.StrVal = (string)dto.Value!;
                break;
            default:
                break;
        }

        if (dto.WakeAfter.HasValue)
        {
            call.WakeAfterSecs = dto.WakeAfter.Value;
        }

        var deviceId = lookup!.Get<Guid>(DEVICE_ID);

        // we capture the service in a local variable
        // this way we avoid capturing 'this' in the lambda
        var service = _chirpstackService;

        // this is deferred call, so we use deviceId instead of DevEui
        // so the automation always points to the correct device even if DevEui changes
        return (token) => service.CallFunctionAsync(deviceId, call, token);

        // TODO: consider catching the result and logging it, so we can see if the call failed
    }
}
