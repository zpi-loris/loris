using Loris.Dtos.Function;
using Loris.Dtos.Sensor;
using Loris.Models;

namespace Loris.Dtos.Device;

public record DeviceDetailsResponse
{
    public required Guid DeviceId { get; init; }
    public required DevEui DevEui { get; init; }
    public required string DeviceName { get; init; }
    public required string FriendlyName { get; init; }
    public required DateTime LastSeen { get; init; }
    public required DeviceStatus DeviceStatus { get; init; }
    public IReadOnlyList<FunctionResponse> Functions { get; init; } = [];
    public IReadOnlyList<SensorResponse> Sensors { get; init; } = [];

    public static DeviceDetailsResponse FromModel(Models.Device device)
    {
        return new DeviceDetailsResponse
        {
            DeviceId = device.DeviceId,
            DevEui = device.DevEui,
            DeviceName = device.DeviceName,
            FriendlyName = device.FriendlyName,
            LastSeen = device.LastSeen,
            DeviceStatus = device.DeviceStatus,
            Functions = device.Functions.Select(FunctionResponse.FromModel).ToList(),
            Sensors = device.Sensors.Select(SensorResponse.FromModel).ToList(),
        };
    }
}
