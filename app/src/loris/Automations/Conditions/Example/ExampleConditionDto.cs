namespace Loris.Automations.Conditions.Example;

// see Triggers/Example for full example
public record ExampleConditionDto(string Name) : IConditionDto
{
    public const string Discriminator = "example";
    public string ServiceKey => Discriminator;
};
