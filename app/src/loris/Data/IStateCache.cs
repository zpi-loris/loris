using System.Diagnostics.CodeAnalysis;
using Loris.Models;
using Loris.Protocol;

namespace Loris.Data;

///<summary>
/// Holds the state of all approved devices
///</summary>
public interface IStateCache
{
    public bool TryGetDevice(DevEui devEui, [MaybeNullWhen(false)] out Device device);
    public bool TryGetSensor(
        DevEui devEui,
        int deviceSensorId,
        [MaybeNullWhen(false)] out Models.Sensor sensor
    );
    public bool TryGetSensorLastReading(
        Guid sensorId,
        [MaybeNullWhen(false)] out SensorReading reading
    );

    public void AddOrUpdateReading(Guid sensorId, Report report, DateTime datetime);

    public void AddOrUpdateReading(SensorReading reading);

    public DevEui this[Guid DeviceId] { get; }

    public Device this[DevEui devEui] { get; set; }

    public Models.Sensor this[(DevEui devEui, int deviceSensorId) sensorMapping] { get; set; }
}
