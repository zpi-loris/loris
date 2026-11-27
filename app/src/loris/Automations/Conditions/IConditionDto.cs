using System.Text.Json.Serialization;
using Loris.Automations.Conditions.Example;
using Loris.Automations.Conditions.Time;

namespace Loris.Automations.Conditions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ExampleConditionDto), typeDiscriminator: ExampleConditionDto.Discriminator)]
[JsonDerivedType(typeof(TimeConditionDto), typeDiscriminator: TimeConditionDto.Discriminator)]
public interface IConditionDto : IAutomationBlock { }
