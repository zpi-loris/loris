using Chirpstack.Api;
using Google.Protobuf;
using Grpc.Core;
using Loris.Data;
using Loris.Extensions;
using Loris.Logging;
using Loris.Protocol;
using Loris.Types;
using Microsoft.EntityFrameworkCore;

namespace Loris.Automations.Actions.DeviceCall;

public class DeviceCallActionCompiler(
    ILogger<DeviceCallActionCompiler> logger,
    IDbContextFactory<LorisDbContext> ctxFactory,
    DeviceService.DeviceServiceClient client
) : ActionCompilerBase<DeviceCallActionDto>
{
    private readonly ILogger<DeviceCallActionCompiler> _logger = logger;
    private readonly IDbContextFactory<LorisDbContext> _ctxFactory = ctxFactory;
    private readonly DeviceService.DeviceServiceClient _downlinkClient = client;

    // lookups
    private const string FUNCTION_ID = "FunctionId";
    private const string DEV_EUI = "DevEui";
    private const string FUNC_ARG_TYPE = "FuncArgType";

    protected override async Task<ValidationResult> ValidateAsync(
        DeviceCallActionDto dto,
        CancellationToken cancellationToken
    )
    {
        using var ctx = await _ctxFactory.CreateDbContextAsync(cancellationToken);

        var device = await ctx
            .Devices.Where(d => d.DeviceStatus == Loris.Models.DeviceStatus.ACTIVE)
            .SingleOrDefaultAsync(d => d.FriendlyName == dto.DeviceName, cancellationToken);

        if (device is null)
        {
            return ValidationResult.Failure([
                new BlockError(
                    nameof(dto.DeviceName),
                    dto.DeviceName,
                    "There is no device with given Friendly Name"
                ),
            ]);
        }

        var function = await ctx.Functions.SingleOrDefaultAsync(
            f => f.DevEui == device.DevEui && f.Name == dto.FunctionName,
            cancellationToken
        );

        if (function is null)
        {
            return ValidationResult.Failure([
                new BlockError(
                    nameof(dto.FunctionName),
                    dto.FunctionName,
                    $"There is no function with given Friendly Name for device {dto.DeviceName}"
                ),
            ]);
        }

        Dictionary<string, object> lookup = [];

        // add verified values
        lookup.Add(FUNCTION_ID, function.FunctionId);
        lookup.Add(DEV_EUI, device.DevEui);

        var isValidValue = function.ArgType switch
        {
            FuncArgType.Float => float.TryParse(dto.Value, out var _),
            FuncArgType.Int => int.TryParse(dto.Value, out var _),
            FuncArgType.Bool => bool.TryParse(dto.Value, out var _),
            _ => true,
        };

        if (!isValidValue)
        {
            ValidationResult.Failure([
                new BlockError(
                    nameof(dto.Value),
                    dto.Value,
                    $"Function accepts values of type {function.ArgType}"
                ),
            ]);
        }

        lookup.Add(FUNC_ARG_TYPE, function.ArgType);

        return ValidationResult.Success(lookup);
    }

    protected override Func<CancellationToken, Task> Compile(
        DeviceCallActionDto dto,
        LookupContext? lookup
    )
    {
        Call call = new() { FunctionId = lookup!.Get<int>(FUNCTION_ID) };

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

        return (token) =>
            RequestDownlink(lookup!.Get<DevEui>(DEV_EUI), new Downlink { Call = call }, token);
    }

    private async Task RequestDownlink(
        DevEui devEui,
        Downlink message,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await _downlinkClient.EnqueueDownlinkAsync(message, devEui, cancellationToken);
        }
        catch (RpcException ex)
        {
            _logger.DownlinkFailed(devEui, ex.Message);
        }
    }
}
