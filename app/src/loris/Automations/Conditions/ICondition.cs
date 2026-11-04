using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using Loris.Protocol;
using Loris.Types;

namespace Loris.Automations.Conditions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TestCondition), "test")]
public interface ICondition : IAutomationBlock
{
    public Func<ReadOnlyDictionary<DevEui, Report>, bool> CompileCondition();
}
