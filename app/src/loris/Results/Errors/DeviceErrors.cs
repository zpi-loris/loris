using Loris.Models;

namespace Loris.Results.Errors;

public static class DeviceErrors
{
    public static Error NotFound(Guid deviceId) =>
        Error.NotFound("Devices.NotFound", $"No device was found with the id {deviceId}.");

    public static Error NotFound(string name) =>
        Error.NotFound("Devices.NotFound", $"No device was found with the friendly name {name}.");

    public static Error NotFound(DevEui devEui) =>
        Error.NotFound("Devices.NotFound", $"No device was found with the devEUI {devEui}.");

    public static Error SubstituteNotFound(Guid deviceId) =>
        Error.Invalid(
            "Devices.SubstituteNotFound",
            $"Substitute device with id {deviceId} was not found"
        );

    public static Error SubstituteNotFound(DevEui devEui) =>
        Error.Invalid(
            "Devices.SubstituteNotFound",
            $"Substitute device with devEUI {devEui} was not found"
        );

    public static Error SubstituteNotFound(string name) =>
        Error.Invalid(
            "Devices.SubstituteNotFound",
            $"Substitute device with friendly name {name} was not found"
        );

    public static Error SubstituteNotPending(string device) =>
        Error.Conflict(
            "Devices.SubstituteNotPending",
            $"Substitute device {device} must be in a pending state to substitute"
        );

    public static Error SubstituteMismatch(string device) =>
        Error.Conflict(
            "Devices.SubstituteMismatch",
            $"Substitute devices {device} differs with capabilities from original"
        );

    public static Error SubstituteTheSame() =>
        Error.Invalid("Devices.SubstituteTheSame", "Can't replace a device with itself");

    public static Error Disconnected(string device) =>
        Error.Conflict("Devices.NotConnected", $"Device {device} is  disconnected");

    public static Error Pending(string device) =>
        Error.Conflict("Devices.Pending", $"Device {device} is  pending");

    public static Error Ignored(string device) =>
        Error.Conflict("Devices.Ignored", $"Device {device} is  ignored");

    public static Error Active(string device) =>
        Error.Conflict("Devices.Active", $"Device {device} is  active");

    public static Error PendingButIgnored(string device) =>
        Error.Conflict("Devices.PendingButIgnored", $"Device {device} is pending but ignored");

    public static Error AlreadyActive(string device) =>
        Error.Conflict("Devices.AlreadyActive", $"Device {device} is already active.");

    public static Error AlreadyIgnored(string device) =>
        Error.Conflict("Devices.AlreadyIgnored", $"Device {device} is already ignored.");

    public static Error AlreadyDisconnected(string device) =>
        Error.Conflict("Devices.AlreadyDisconnected", $"Device {device} is already disconnected.");

    public static Error FriendlyNameTaken(string name) =>
        Error.Conflict(
            "Devices.FriendlyNameAlreadyExists",
            $"Device {name} friendly name already exists"
        );

    public static Error InvalidSubstituteKey(object key) =>
        Error.Invalid("Devices.InvalidSubstituteKey", $"Device substitute key: {key} is invalid");

    public static Error UnknownStatus(string device) =>
        new("Devices.UnknownStatus", $"Device {device} has an unknown status.", ErrorType.Failure);
}
