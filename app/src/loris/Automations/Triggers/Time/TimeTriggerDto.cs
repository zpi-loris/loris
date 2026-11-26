using System.Text.Json.Serialization;
using Loris.Automations.Helpers;
using Loris.Json;

namespace Loris.Automations.Triggers.Time;

public record TimeTriggerDto : ITriggerDto
{
    [JsonOneOfTypes(typeof(TimeSpan), typeof(TimeMap))]
    public required object Time { get; init; }

    [JsonIgnore]
    public string ServiceKey => Discriminator;
    public const string Discriminator = "time";
}
