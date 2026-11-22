using Chirpstack.Api;
using Grpc.Core;
using Loris.Data;
using Loris.Extensions;
using Loris.Logging;
using Loris.Models;
using Loris.Protocol;
using Loris.Results;
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
            .Devices.Where(d => d.DeviceStatus == Models.DeviceStatus.ACTIVE)
            .SingleOrDefaultAsync(d => d.FriendlyName == dto.DeviceName, cancellationToken);

        if (device is null)
        {
            return BlockValidationResult.Failure([
                new BlockError(
                    nameof(dto.DeviceName),
                    dto.DeviceName,
                    "There is no device with given Friendly Name"
                ),
            ]);
        }

        var function = await ctx.Functions.SingleOrDefaultAsync(
            f => f.DeviceId == device.DeviceId && f.Name == dto.FunctionName,
            cancellationToken
        );

        if (function is null)
        {
            return BlockValidationResult.Failure([
                new BlockError(
                    nameof(dto.FunctionName),
                    dto.FunctionName,
                    $"There is no function with given Friendly Name for device {dto.DeviceName}"
                ),
            ]);
        }

        var isValidValue = function.ArgType switch
        {
            FuncArgType.Float => float.TryParse(dto.Value, out var _),
            FuncArgType.Int => int.TryParse(dto.Value, out var _),
            FuncArgType.Bool => bool.TryParse(dto.Value, out var _),
            _ => true,
        };

        if (!isValidValue)
        {
            BlockValidationResult.Failure([
                new BlockError(
                    nameof(dto.Value),
                    dto.Value,
                    $"Function accepts values of type {function.ArgType}"
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
                call.FloatVal = float.Parse(dto.Value);
                break;
            case FuncArgType.Int:
                call.IntVal = int.Parse(dto.Value);
                break;
            case FuncArgType.Bool:
                call.BoolVal = bool.Parse(dto.Value);
                break;
            case FuncArgType.String:
                call.StrVal = dto.Value;
                break;
            default:
                break;
        }

        if (dto.WakeAfter.HasValue)
        {
            call.WakeAfterSecs = dto.WakeAfter.Value;
        }

        var deviceId = lookup!.Get<Guid>(DEVICE_ID);

        // this is deferred call, so we use deviceId instead of DevEui
        // so the automation always points to the correct device even if DevEui changes
        //
        // // TODO: consider catching the result and logging it, so we can see if the call failed
        return (token) => _chirpstackService.CallFunctionAsync(deviceId, call, token);
    }
}
