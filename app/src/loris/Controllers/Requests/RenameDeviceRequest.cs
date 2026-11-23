namespace Loris.Controllers.Requests;

public record RenameDeviceRequest
{
    public required string NewFriendlyName { get; set; }
}
