namespace Loris.Results.Errors;

public static class DownlinkErrors
{
    public static Error Failed(string message) =>
        new("Downlink.Failed", $"Failed to send downlink: {message}", ErrorType.Upstream);

    public static Error DeadlineExceeded() =>
        new("Downlink.Deadline", "LoraWAN network server connection timeout", ErrorType.Timeout);
}
