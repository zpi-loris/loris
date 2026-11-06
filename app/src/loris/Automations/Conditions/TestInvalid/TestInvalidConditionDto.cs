namespace Loris.Automations.Conditions.TestInvalid;

public record TestInvalidConditionDto(string Name) : IConditionDto
{
    public const string Discriminator = "test-invalid";
    public string ServiceKey => Discriminator;
};
