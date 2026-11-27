using System.Text.Json.Serialization;
using Loris.Protocol;
using Microsoft.EntityFrameworkCore;

namespace Loris.Models;

[Comment("Devices sensors exposed via capabilities")]
public partial class Sensor
{
    /// <summary>
    /// Artificial primary key
    /// </summary>
    public Guid SensorId { get; set; }

    /// <summary>
    /// Locally unique ID in a single device
    /// </summary>
    public int DeviceSensorId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? Unit { get; set; }

    public SensorValueType ValueType { get; set; }

    public SensorSemantics Semantics { get; set; }

    public Guid DeviceId { get; set; }

    //
    // REFERENCES
    //

    [JsonIgnore]
    public Device Device { get; set; } = null!;

    [JsonIgnore]
    public ICollection<SensorReading> SensorReadings { get; } = [];

    //
    // Proto helpers
    //

    public static Sensor FromProto(Guid deviceId, Protocol.Sensor sensor)
    {
        return new Sensor()
        {
            DeviceId = deviceId,
            DeviceSensorId = sensor.Id,
            Name = sensor.Name,
            Description = sensor.Description,
            Unit = sensor.HasUnit ? sensor.Unit : default,
            ValueType = sensor.ValueType,
            Semantics = sensor.Semantics,
        };
    }

    public bool EqualsWithDefinition(Sensor other)
    {
        if (
            DeviceSensorId != other.DeviceSensorId
            || Name != other.Name
            || Description != other.Description
            || Unit != other.Unit
            || ValueType != other.ValueType
            || Semantics != other.Semantics
        )
        {
            return false;
        }

        return true;
    }
}
