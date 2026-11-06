namespace Loris.Automations.Actions.TestValid;

public record TestValidActionDto(string Name) : IActionDto
{
    public const string Discriminator = "test-valid";
    public string ServiceKey => Discriminator;
}
