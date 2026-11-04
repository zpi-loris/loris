namespace Loris.Automations.Triggers.Test;

public record TestTriggerDto(string Name) : ITriggerDto
{
    public const string Discriminator = "test";
    public string ServiceKey => Discriminator;
}
