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
    private const string DEV_EUI = "DevEui";
    private const string SENSOR_VALUE_TYPE = "SensorValueType";

    protected override async Task<ValidationResult> ValidateAsync(
        SensorCrossedTriggerDto dto,
        CancellationToken cancellationToken
    )
    {
        using var ctx = await _ctxFactory.CreateDbContextAsync(cancellationToken);

        var device = await ctx
            .Devices.Where(d => d.DeviceStatus == DeviceStatus.ACTIVE)
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

        var sensor = await ctx.Sensors.SingleOrDefaultAsync(
            s => s.DevEui == device.DevEui && s.Name == dto.SensorName,
            cancellationToken
        );

        if (sensor is null)
        {
            return ValidationResult.Failure([
                new BlockError(
                    nameof(dto.SensorName),
                    dto.SensorName,
                    $"There is no sensor with given Friendly Name for device {dto.DeviceName}"
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
            return ValidationResult.Failure([
                new BlockError(
                    nameof(dto.SensorName),
                    dto.SensorName,
                    $"Sensor of type {sensor.ValueType} does not support threshold crossing"
                ),
            ]);
        }

        var isValidValue = sensor.ValueType switch
        {
            SensorValueType.Float => float.TryParse(dto.Value, out _),
            SensorValueType.Int => int.TryParse(dto.Value, out _),
            _ => false,
        };

        if (!isValidValue)
        {
            return ValidationResult.Failure([
                new BlockError(
                    nameof(dto.Value),
                    dto.Value,
                    $"Sensor accepts values of type: {sensor.ValueType}"
                ),
            ]);
        }

        return ValidationResult.Success(
            new Dictionary<string, object>
            {
                { SENSOR_ID, sensor.SensorId },
                { DEV_EUI, device.DevEui },
                { SENSOR_VALUE_TYPE, sensor.ValueType },
            }
        );
    }

    protected override (IEvent Event, Func<IEventMessage, bool> Filter) Compile(
        SensorCrossedTriggerDto dto,
        LookupContext? lookup
    )
    {
        var devEui = lookup!.Get<DevEui>(DEV_EUI);
        var sensorId = lookup!.Get<int>(SENSOR_ID);
        var valueType = lookup!.Get<SensorValueType>(SENSOR_VALUE_TYPE);

        IEvent newEvent = new SensorEvent { DevEui = devEui, SensorID = sensorId };

        Func<IEventMessage, bool> filter = valueType switch
        {
            SensorValueType.Float => CreateFloatFilter(float.Parse(dto.Value), dto.TopBottom),
            SensorValueType.Int => CreateIntFilter(int.Parse(dto.Value), dto.TopBottom),
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
