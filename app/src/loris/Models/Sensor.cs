using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Loris.Models;

[Table("sensors")]
[Index("SensorId", "DeviceId", Name = "sensors_unique_0", IsUnique = true)]
public partial class Sensor
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("sensor_id")]
    public int SensorId { get; set; }

    [Column("device_id")]
    public Guid DeviceId { get; set; }

    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string Description { get; set; } = null!;

    [Column("unit")]
    public string? Unit { get; set; }

    [ForeignKey("DeviceId")]
    [InverseProperty("Sensors")]
    public virtual Device Device { get; set; } = null!;

    [InverseProperty("Sensor")]
    public virtual ICollection<SensorReading> SensorReadings { get; set; } = [];

    // TODO: UPDATE MODEL
    //
    // Enum column 'public.sensors.value_type' cannot be scaffolded, define a CLR enum type and add the property manually.
    //
    // Enum column 'public.sensors.value_data_type' cannot be scaffolded, define a CLR enum type and add the property manually.
}
