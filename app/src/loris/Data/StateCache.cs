using System.Diagnostics.CodeAnalysis;
using Loris.Models;
using Loris.Protocol;

namespace Loris.Data;

public sealed class StateCache : IStateCache
{
    private readonly Dictionary<DevEui, Device> _devEuiToDevice = [];
    private readonly Dictionary<Guid, DevEui> _deviceToDevEui = [];
    private readonly Dictionary<(DevEui, int), Models.Sensor> _sensorMapping = [];
    private readonly Dictionary<Guid, SensorReading> _lastReadings = [];

    public Device this[DevEui devEui]
    {
        get => _devEuiToDevice[devEui];
        set
        {
            // check if some device (value) already points to some devEUI
            if (_deviceToDevEui.TryGetValue(value.DeviceId, out var oldDevEui))
            {
                // This means, that we have replaced the devEUI (replaced a physical device)
                // with a new one. All the mappings must be reassigned, and old devEUI removed from cache.

                // remove old devEUI mapping
                _devEuiToDevice.Remove(oldDevEui);

                // point with deviceId to new devEUI
                _deviceToDevEui[value.DeviceId] = devEui;

                // replace the sensor mapping with wrong devEUI
                var oldDevEuiKeys = _sensorMapping.Keys.Where(k => k.Item1 == oldDevEui);
                foreach (var (_, deviceSensorId) in oldDevEuiKeys)
                {
                    var sensor = _sensorMapping[(oldDevEui, deviceSensorId)];
                    _sensorMapping.Remove((oldDevEui, deviceSensorId));
                    _sensorMapping[(devEui, deviceSensorId)] = sensor;
                }
            }

            _devEuiToDevice[devEui] = value;
        }
    }
    public Models.Sensor this[(DevEui devEui, int deviceSensorId) sensorMapping]
    {
        get => _sensorMapping[sensorMapping];
        set => _sensorMapping[sensorMapping] = value;
    }

    public DevEui this[Guid deviceId]
    {
        get => _deviceToDevEui[deviceId];
    }

    public void AddOrUpdateReading(Guid sensorId, Report report, DateTime datetime) =>
        _lastReadings[sensorId] = SensorReading.FromProto(sensorId, datetime, report);

    public void AddOrUpdateReading(SensorReading reading) =>
        _lastReadings[reading.SensorId] = reading;

    public bool TryGetDevice(DevEui devEui, [MaybeNullWhen(false)] out Device device)
    {
        return _devEuiToDevice.TryGetValue(devEui, out device);
    }

    public bool TryGetSensor(
        DevEui devEui,
        int deviceSensorId,
        [MaybeNullWhen(false)] out Models.Sensor sensor
    ) => _sensorMapping.TryGetValue((devEui, deviceSensorId), out sensor);

    public bool TryGetSensorLastReading(
        Guid sensorId,
        [MaybeNullWhen(false)] out SensorReading reading
    ) => _lastReadings.TryGetValue(sensorId, out reading);
}
