using System.Text.Json.Serialization;
using Loris.Automations.Conditions.Example;

namespace Loris.Automations.Conditions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ExampleConditionDto), typeDiscriminator: ExampleConditionDto.Discriminator)]
public interface IConditionDto : IAutomationBlock { }
