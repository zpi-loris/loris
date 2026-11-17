using System.Text.Json.Serialization;

namespace Loris.Automations.Triggers.SensorCrossed;

public record SensorCrossedTriggerDto(
    string DeviceName,
    string SensorName,
    string Value,
    bool TopBottom
) : ITriggerDto
{
    public const string Discriminator = "sensor-crossed";

    [JsonIgnore]
    public string ServiceKey => Discriminator;
}
