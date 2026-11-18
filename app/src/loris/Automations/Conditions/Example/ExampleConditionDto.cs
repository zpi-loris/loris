using System.Text.Json.Serialization;

namespace Loris.Automations.Conditions.Example;

// see Triggers/Example for full example
public record ExampleConditionDto(string Name) : IConditionDto
{
    public const string Discriminator = "example";

    [JsonIgnore]
    public string ServiceKey => Discriminator;
};
