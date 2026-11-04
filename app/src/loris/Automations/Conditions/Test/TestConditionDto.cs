namespace Loris.Automations.Conditions.Test;

public record TestConditionDto(string Name) : IConditionDto
{
    public const string Discriminator = "test";
    public string ServiceKey => Discriminator;
};
