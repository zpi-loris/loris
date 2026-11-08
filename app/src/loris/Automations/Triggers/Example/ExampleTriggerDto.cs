namespace Loris.Automations.Triggers.Example;

public record ExampleTriggerDto(string Name, int Value) : ITriggerDto
{
    public const string Discriminator = "example";
    public string ServiceKey => Discriminator;
}
