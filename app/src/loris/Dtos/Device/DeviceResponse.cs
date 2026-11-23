using Loris.Models;

namespace Loris.Dtos.Device;

public record DeviceResponse
{
    public required Guid DeviceId { get; init; }
    public required DevEui DevEui { get; init; }
    public required string DeviceName { get; init; }
    public required string FriendlyName { get; init; }
    public required DateTime LastSeen { get; init; }
    public required DeviceStatus DeviceStatus { get; init; }

    public static DeviceResponse FromModel(Models.Device device)
    {
        return new DeviceResponse
        {
            DeviceId = device.DeviceId,
            DevEui = device.DevEui,
            DeviceName = device.DeviceName,
            FriendlyName = device.FriendlyName,
            LastSeen = device.LastSeen,
            DeviceStatus = device.DeviceStatus,
        };
    }
}
