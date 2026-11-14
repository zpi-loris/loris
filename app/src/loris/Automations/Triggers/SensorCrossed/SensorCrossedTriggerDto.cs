namespace Loris.Automations.Triggers.SensorCrossed;

public record SensorCrossedTriggerDto(
    string DeviceName,
    string SensorName,
    string Value,
    bool TopBottom
) : ITriggerDto
{
    public const string Discriminator = "sensor-crossed";
    public string ServiceKey => Discriminator;
}
