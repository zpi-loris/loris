namespace Loris.Dtos.Device;

public record RenameDeviceRequest
{
    public required string NewFriendlyName { get; init; }
}
