using System.Diagnostics.CodeAnalysis;
using Loris.Models;
using Loris.Protocol;
using Loris.Types;

namespace Loris.Data;

///<summary>
/// Holds the state of all approved devices
///</summary>
public interface IStateCache
{
    public bool IsDeviceAccepted(DevEui devEui);
    public bool TryGetSensorLastSeen(
        DevEui devEui,
        int sensorId,
        [MaybeNullWhen(false)] out DateTime state
    );
    public bool TryGetSensorLastState(
        DevEui devEui,
        int sensorId,
        [NotNullWhen(true)] out Report? state
    );
    public void UpdateSensors(DevEui devEui, Report report, DateTime time);
    public void UpdateSensors(DevEui devEui, SensorReading model);
    public void UpdateDevices(DevEui devEui);
}
