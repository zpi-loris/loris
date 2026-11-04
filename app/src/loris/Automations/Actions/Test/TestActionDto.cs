namespace Loris.Automations.Actions.Test;

public record TestActionDto(string Name) : IActionDto
{
    public const string Discriminator = "test";
    public string ServiceKey => Discriminator;
}
