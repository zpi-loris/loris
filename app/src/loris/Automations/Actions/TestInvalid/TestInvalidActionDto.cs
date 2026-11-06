namespace Loris.Automations.Actions.TestInvalid;

public record TestInvalidActionDto(string Name) : IActionDto
{
    public const string Discriminator = "test-invalid";
    public string ServiceKey => Discriminator;
}
