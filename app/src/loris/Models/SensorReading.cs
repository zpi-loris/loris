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
}
