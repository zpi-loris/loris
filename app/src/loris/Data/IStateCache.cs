using System.Diagnostics.CodeAnalysis;
using Loris.Models;
using Loris.Protocol;

namespace Loris.Data;

///<summary>
/// Holds the state of all approved devices using domain models.<br/>
/// It allows to retrieve domain model data fast, using on-wire data from loris protocol
///</summary>
public interface IStateCache
{
    /// <summary>
    /// Tries to get a device Domain model instance, represented by devEUI
    /// </summary>
    /// <remarks>
    /// The instance doesn't include related data
    /// </remarks>
    public bool TryGetDevice(DevEui devEui, [MaybeNullWhen(false)] out Device device);

    /// <summary>
    /// Tries to get a sensor Domain model instance, represented by devEUI and deviceSensorId
    /// </summary>
    /// <remarks>
    /// The instance doesn't include related data
    /// </remarks>
    public bool TryGetSensor(
        DevEui devEui,
        int deviceSensorId,
        [MaybeNullWhen(false)] out Models.Sensor sensor
    );

    /// <summary>
    /// Tries to get the most recent instance of sensor reading domain model, represented by sensor's domain SensorId
    /// </summary>
    /// <remarks>
    /// The function does not differentiate if the sensor exists or has not sent any data yet.
    /// </remarks>
    public bool TryGetSensorLastReading(
        Guid sensorId,
        [MaybeNullWhen(false)] out SensorReading reading
    );

    /// <summary>
    /// Updates sensor's last reading using on-wire data from loris protocol
    /// </summary>
    public void AddOrUpdateReading(Guid sensorId, Report report, DateTime datetime);

    /// <summary>
    /// Updates sensor's last reading using domain model instance
    /// </summary>
    public void AddOrUpdateReading(SensorReading reading);

    /// <summary>
    /// Retrieves the devEUI that is associated with the domain model DeviceId
    /// </summary>
    public DevEui this[Guid DeviceId] { get; }

    /// <summary>
    /// Indexer allows to update/add or retrieve device domain model instance, represented by devEUI
    /// </summary>
    public Device this[DevEui devEui] { get; set; }

    /// <summary>
    /// Indexer allows to update/add or retrieve sensor domain model instance represented by devEUI and deviceSensorId
    /// </summary>
    public Models.Sensor this[(DevEui devEui, int deviceSensorId) sensorMapping] { get; set; }
}
