using System.ComponentModel;
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

    public static SensorReading FromProto(DevEui devEui, DateTime dateTime, Report report)
    {
        var reading = new SensorReading()
        {
            DevEui = devEui,
            SensorId = report.SensorId,
            Time = dateTime,
        };

        switch (report.ValueCase)
        {
            case Report.ValueOneofCase.FloatVal:
                reading.FloatValue = report.FloatVal;
                break;
            case Report.ValueOneofCase.IntVal:
                reading.IntValue = report.IntVal;
                break;
            case Report.ValueOneofCase.BoolVal:
                reading.BoolValue = report.BoolVal;
                break;
            case Report.ValueOneofCase.StrVal:
                reading.StringValue = report.StrVal;
                break;
            case Report.ValueOneofCase.None:
            default:
                throw new InvalidEnumArgumentException(
                    "Sensor report does not have any value associated"
                );
        }
        return reading;
    }

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
