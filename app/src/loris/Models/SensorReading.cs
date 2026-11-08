using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Loris.Models;

[PrimaryKey("SensorId", "Time")]
[Table("sensor_readings")]
public partial class SensorReading
{
    [Key]
    [Column("sensor_id")]
    public Guid SensorId { get; set; }

    [Key]
    [Column("time")]
    public DateTime Time { get; set; }

    [Column("int_value")]
    public int? IntValue { get; set; }

    [Column("float_value")]
    public float? FloatValue { get; set; }

    [Column("string_value")]
    public string? StringValue { get; set; }

    [Column("bool_value")]
    public bool? BoolValue { get; set; }

    [ForeignKey("SensorId")]
    [InverseProperty("SensorReadings")]
    public virtual Sensor Sensor { get; set; } = null!;
}
