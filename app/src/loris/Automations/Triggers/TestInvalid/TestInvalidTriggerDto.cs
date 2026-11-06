namespace Loris.Automations.Triggers.TestInvalid;

public record TestInvalidTriggerDto(string Name) : ITriggerDto
{
    public const string Discriminator = "test-invalid";
    public string ServiceKey => Discriminator;
}
