using Loris.Protocol;
using Loris.Types;
using Microsoft.EntityFrameworkCore;

namespace Loris.Models;

[Comment("Devices sensors exposed via capabilities")]
public partial class Sensor
{
    public DevEui DevEui { get; set; }

    public int SensorId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? Unit { get; set; }

    public SensorValueType ValueType { get; set; }

    public SensorSemantics Semantics { get; set; }

    //
    // REFERENCES
    //

    public Device Device { get; set; } = null!;

    public ICollection<SensorReading> SensorReadings { get; } = [];

    //
    // Proto helpers
    //

    public static Sensor FromProto(DevEui devEui, Protocol.Sensor sensor)
    {
        return new Sensor()
        {
            DevEui = devEui,
            SensorId = sensor.Id,
            Name = sensor.Name,
            Description = sensor.Description,
            Unit = sensor.HasUnit ? sensor.Unit : default,
            ValueType = sensor.ValueType,
            Semantics = sensor.Semantics,
        };
    }
}
