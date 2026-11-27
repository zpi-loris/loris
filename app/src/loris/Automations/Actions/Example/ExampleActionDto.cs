using System.Text.Json.Serialization;

namespace Loris.Automations.Actions.Example;

// see Triggers/Example for full example
public record ExampleActionDto(string Name) : IActionDto
{
    public const string Discriminator = "example";

    [JsonIgnore]
    public string ServiceKey => Discriminator;
}
