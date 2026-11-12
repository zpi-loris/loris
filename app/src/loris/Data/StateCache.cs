using System.Diagnostics.CodeAnalysis;
using Loris.Models;
using Loris.Protocol;
using Loris.Types;

namespace Loris.Data;

public sealed class StateCache : IStateCache
{
    private readonly HashSet<DevEui> _devices = [];
    private readonly Dictionary<(DevEui, int), (Report, DateTime)> _sensorsCache = [];

    public bool TryGetSensorLastState(
        DevEui devEui,
        int sensorId,
        [NotNullWhen(true)] out Report? state
    )
    {
        if (!_sensorsCache.TryGetValue((devEui, sensorId), out var value))
        {
            state = default;
            return false;
        }
        state = value.Item1;
        return true;
    }

    public bool TryGetSensorLastSeen(
        DevEui devEui,
        int sensorId,
        [MaybeNullWhen(false)] out DateTime state
    )
    {
        if (!_sensorsCache.TryGetValue((devEui, sensorId), out var value))
        {
            state = default;
            return false;
        }
        state = value.Item2;
        return true;
    }

    public bool IsDeviceAccepted(DevEui devEui) => _devices.Contains(devEui);

    public void UpdateSensors(DevEui devEui, Report report, DateTime time) =>
        _sensorsCache[(devEui, report.SensorId)] = (report, time);

    public void UpdateSensors(DevEui devEui, SensorReading model) =>
        _sensorsCache[(devEui, model.SensorId)] = (model.ToReport(), model.Time);

    public void UpdateDevices(DevEui devEui) => _devices.Add(devEui);
}
