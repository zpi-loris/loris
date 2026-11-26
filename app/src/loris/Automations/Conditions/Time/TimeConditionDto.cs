using System.Text.Json.Serialization;
using Loris.Automations.Helpers;
using Loris.Json;

namespace Loris.Automations.Conditions.Time;

public record TimeConditionDto : IConditionDto
{
    [JsonOneOfTypes(typeof(TimeSpan), typeof(TimeMap))]
    public object? After { get; init; }

    [JsonOneOfTypes(typeof(TimeSpan), typeof(TimeMap))]
    public object? Before { get; init; }

    [JsonIgnore]
    public string ServiceKey => Discriminator;
    public const string Discriminator = "time";
};
