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

    public Device Device { get; set; } = null!;

    public ICollection<SensorReading> SensorReadings { get; } = [];

    // TODO: UPDATE MODEL
    //
    // Enum column 'public.sensors.value_type' cannot be scaffolded, define a CLR enum type and add the property manually.
    //
    // Enum column 'public.sensors.value_data_type' cannot be scaffolded, define a CLR enum type and add the property manually.
}
