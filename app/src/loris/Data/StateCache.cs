using Loris.Models;
using Loris.Protocol;
using Loris.Types;

namespace Loris.Data;

public sealed record CacheEntry(Report? Report, DateTime Time)
{
    public static CacheEntry FromModel(SensorReading sensorReading)
    {
        var entry = new CacheEntry(null, sensorReading.Time);

        if (sensorReading.FloatValue.HasValue)
        {
            entry = entry with
            {
                Report = new Report
                {
                    SensorId = sensorReading.SensorId,
                    FloatVal = sensorReading.FloatValue.Value,
                },
            };
        }
        else if (sensorReading.IntValue.HasValue)
        {
            entry = entry with
            {
                Report = new Report
                {
                    SensorId = sensorReading.SensorId,
                    IntVal = sensorReading.IntValue.Value,
                },
            };
        }
        else if (sensorReading.BoolValue.HasValue)
        {
            entry = entry with
            {
                Report = new Report
                {
                    SensorId = sensorReading.SensorId,
                    BoolVal = sensorReading.BoolValue.Value,
                },
            };
        }
        else if (sensorReading.StringValue is not null)
        {
            entry = entry with
            {
                Report = new Report
                {
                    SensorId = sensorReading.SensorId,
                    StrVal = sensorReading.StringValue,
                },
            };
        }
        return entry;
    }
}

public sealed class StateCache
{
    private readonly Dictionary<DevEui, CacheEntry> state = [];
}
