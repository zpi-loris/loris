using System.Text.Json.Serialization;

namespace Loris.Automations.Actions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TestAction), typeDiscriminator: "test")]
public interface IAction : IAutomationBlock
{
    public Action CompileAction();
}
