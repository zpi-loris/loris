using Loris.Types;

namespace Loris.Results.Errors;

public static class DeviceErrors
{
    public static Error NotFound(string name) =>
        Error.NotFound("Devices.NotFound", $"No device was found with the friendly name {name}.");

    public static Error NotFound(DevEui devEui) =>
        Error.NotFound("Devices.NotFound", $"No device was found with the devEUI {devEui}.");

    public static Error Disconnected(string device) =>
        Error.Conflict("Devices.NotConnected", $"Device {device} is  disconnected");

    public static Error Pending(string device) =>
        Error.Conflict("Devices.Pending", $"Device {device} is  pending");

    public static Error Ignored(string device) =>
        Error.Conflict("Devices.Ignored", $"Device {device} is  ignored");

    public static Error PendingButIgnored(string device) =>
        Error.Conflict("Devices.PendingButIgnored", $"Device {device} is pending but ignored");

    public static Error AlreadyActive(string device) =>
        Error.Conflict("Devices.AlreadyActive", $"Device {device} is already active.");

    public static Error AlreadyIgnored(string device) =>
        Error.Conflict("Devices.AlreadyIgnored", $"Device {device} is already ignored.");

    public static Error AlreadyDisconnected(string device) =>
        Error.Conflict("Devices.AlreadyDisconnected", $"Device {device} is already disconnected.");

    public static Error UnknownStatus(string device) =>
        new("Devices.UnknownStatus", $"Device {device} has an unknown status.", ErrorType.Failure);
}
