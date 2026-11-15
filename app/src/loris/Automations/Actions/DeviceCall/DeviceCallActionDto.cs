namespace Loris.Automations.Actions.DeviceCall;

public record DeviceCallActionDto(
    string DeviceName,
    string FunctionName,
    string Value,
    int? WakeAfter = null
) : IActionDto
{
    public const string Discriminator = "device-call";
    public string ServiceKey => Discriminator;
}
