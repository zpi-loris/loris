namespace Loris.Automations.Conditions.TestValid;

public record TestValidConditionDto(string Name) : IConditionDto
{
    public const string Discriminator = "test-valid";
    public string ServiceKey => Discriminator;
};
