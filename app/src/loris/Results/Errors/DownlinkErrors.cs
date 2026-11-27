using Loris.Models;

namespace Loris.Results.Errors;

public static class DownlinkErrors
{
    public static Error Failed(string message) =>
        new("Downlink.Failed", $"Failed to send downlink: {message}", ErrorType.Upstream);

    public static Error DeadlineExceeded() =>
        new("Downlink.Deadline", "LoraWAN network server connection timeout", ErrorType.Timeout);

    public static Error Unauthenticated(DevEui devEui) =>
        new(
            "Downlink.Unauthenticated",
            @$"Chirpstack can't enqueue downlinkt to the device {devEui}.
            It either does not exist or it's in another tenant",
            ErrorType.Upstream
        );
}
