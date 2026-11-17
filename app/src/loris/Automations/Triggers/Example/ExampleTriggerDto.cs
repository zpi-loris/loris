using System.Text.Json.Serialization;

namespace Loris.Automations.Triggers.Example;

public record ExampleTriggerDto(string Name, int Value) : ITriggerDto
{
    public const string Discriminator = "example";

    [JsonIgnore]
    public string ServiceKey => Discriminator;
}
