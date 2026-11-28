using Loris.Data;
using Loris.Events;
using Loris.Messages.Event;
using Loris.Models;
using Loris.Protocol;
using Microsoft.EntityFrameworkCore;

namespace Loris.Automations.Triggers.SensorCrossed;

public class SensorCrossedTriggerCompiler(IDbContextFactory<LorisDbContext> ctxFactory)
    : TriggerCompilerBase<SensorCrossedTriggerDto>
{
    private readonly IDbContextFactory<LorisDbContext> _ctxFactory = ctxFactory;

    // lookups
    private const string SENSOR_ID = "SensorId";
    private const string SENSOR_VALUE_TYPE = "SensorValueType";

    protected override async Task<BlockValidationResult> ValidateAsync(
        SensorCrossedTriggerDto dto,
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

        var sensor = await ctx.Sensors.SingleOrDefaultAsync(
            s => s.DeviceId == device.DeviceId && s.Name == dto.Sensor,
            cancellationToken
        );

        if (sensor is null)
        {
            return BlockValidationResult.Failure([
                new BlockError(
                    nameof(dto.Sensor),
                    dto.Sensor,
                    $"[{dto.Device}] device does not have sensor called [{dto.Sensor}]"
                ),
            ]);
        }

        var isSupported = sensor.ValueType switch
        {
            SensorValueType.Float => true,
            SensorValueType.Int => true,
            _ => false,
        };

        if (!isSupported)
        {
            return BlockValidationResult.Failure([
                new BlockError(
                    nameof(dto.Sensor),
                    dto.Sensor,
                    $"Sensor of type {sensor.ValueType} does not support threshold crossing"
                ),
            ]);
        }

        var isValidValue = (sensor.ValueType, dto.Value) switch
        {
            // float accepts int and float value types
            (SensorValueType.Float, _) => true,
            // int must be strictly int
            (SensorValueType.Int, int val) => true,
            _ => false,
        };

        if (!isValidValue)
        {
            return BlockValidationResult.Failure([
                new BlockError(
                    nameof(dto.Value),
                    dto.Value.ToString() ?? "",
                    $"Sensor accepts values of type: {sensor.ValueType}"
                ),
            ]);
        }

        return BlockValidationResult.Success(
            new Dictionary<string, object>
            {
                { SENSOR_ID, sensor.SensorId },
                { SENSOR_VALUE_TYPE, sensor.ValueType },
            }
        );
    }

    protected override (IEvent Event, Func<IEventMessage, bool> Filter) Compile(
        SensorCrossedTriggerDto dto,
        LookupContext? lookup
    )
    {
        var sensorId = lookup!.Get<Guid>(SENSOR_ID);
        var valueType = lookup!.Get<SensorValueType>(SENSOR_VALUE_TYPE);

        IEvent newEvent = new SensorEvent(sensorId);

        Func<IEventMessage, bool> filter = valueType switch
        {
            // here we convert to flaot, from either float or int
            SensorValueType.Float => CreateFloatFilter(Convert.ToSingle(dto.Value), dto.TopBottom),
            // here we are certain it is an int
            SensorValueType.Int => CreateIntFilter((int)dto.Value, dto.TopBottom),
            _ => static (_) => false,
        };

        return (Event: newEvent, Filter: filter);
    }

    private static Func<IEventMessage, bool> CreateFloatFilter(float threshold, bool topBottom) =>
        (ival) =>
        {
            if (ival is not SensorEventMessage { Previous: { } prev, Current: var curr })
            {
                return false;
            }

            return topBottom
                ? prev.FloatVal >= threshold && curr.FloatVal < threshold
                : prev.FloatVal <= threshold && curr.FloatVal > threshold;
        };

    private static Func<IEventMessage, bool> CreateIntFilter(int threshold, bool topBottom) =>
        (ival) =>
        {
            if (ival is not SensorEventMessage { Previous: { } prev, Current: var curr })
            {
                return false;
            }

            return topBottom
                ? prev.IntVal >= threshold && curr.IntVal < threshold
                : prev.IntVal <= threshold && curr.IntVal > threshold;
        };
}
