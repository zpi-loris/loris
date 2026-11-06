namespace Loris.Automations.Triggers.TestValid;

public record TestValidTriggerDto(string Name) : ITriggerDto
{
    public const string Discriminator = "test-valid";
    public string ServiceKey => Discriminator;
}
