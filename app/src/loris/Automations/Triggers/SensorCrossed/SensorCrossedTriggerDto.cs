using System.Text.Json.Serialization;
using Loris.Json;

namespace Loris.Automations.Triggers.SensorCrossed;

public record SensorCrossedTriggerDto : ITriggerDto
{
    public required string Device { get; init; }

    public required string Sensor { get; init; }

    [JsonOneOfTypes(typeof(int), typeof(float))]
    public required object Value { get; init; }

    public bool TopBottom { get; init; } = false;

    [JsonIgnore]
    public string ServiceKey => Discriminator;
    public const string Discriminator = "sensor-crossed";
}
