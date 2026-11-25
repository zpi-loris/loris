using Loris.Protocol;

namespace Loris.Dtos.Sensor;

public record SensorResponse
{
    public required Guid SensorId { get; init; }
    public required int DeviceSensorId { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required SensorValueType ValueType { get; init; }
    public required SensorSemantics Semantics { get; init; }

    public static SensorResponse FromModel(Models.Sensor sensor)
    {
        return new SensorResponse
        {
            SensorId = sensor.SensorId,
            DeviceSensorId = sensor.DeviceSensorId,
            Name = sensor.Name,
            Description = sensor.Description,
            ValueType = sensor.ValueType,
            Semantics = sensor.Semantics,
        };
    }
}
