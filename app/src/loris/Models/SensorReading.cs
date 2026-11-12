using Loris.Protocol;
using Loris.Types;
using Microsoft.EntityFrameworkCore;

namespace Loris.Models;

[Comment("Readings of actuals sensors exposed via capabilities")]
public class SensorReading
{
    public DevEui DevEui { get; set; }

    public int SensorId { get; set; }

    public DateTime Time { get; set; }

    public int? IntValue { get; set; }

    public float? FloatValue { get; set; }

    public string? StringValue { get; set; }

    public bool? BoolValue { get; set; }

    //
    // REFERENCES
    //

    public Sensor Sensor { get; set; } = null!;

    //
    // Proto helpers
    //

    public Report ToReport()
    {
        if (FloatValue.HasValue)
        {
            return new Report { SensorId = SensorId, FloatVal = FloatValue.Value };
        }
        else if (IntValue.HasValue)
        {
            return new Report { SensorId = SensorId, IntVal = IntValue.Value };
        }
        else if (BoolValue.HasValue)
        {
            return new Report { SensorId = SensorId, BoolVal = BoolValue.Value };
        }
        else if (StringValue is not null)
        {
            return new Report { SensorId = SensorId, StrVal = StringValue };
        }
        else
        {
            throw new InvalidOperationException("SensorReading object does not hold any state");
        }
    }
}
