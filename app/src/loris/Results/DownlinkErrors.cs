namespace Loris.Results;

public static class DownlinkErrors
{
    public static Error Failed(string message) =>
        new("Downlink.Failed", $"Failed to send downlink: {message}", ErrorType.Failure);

    public static Error DeadlineExceeded() =>
        new("Downlink.Deadline", "LoraWAN network server connection timeout", ErrorType.Failure);
}
