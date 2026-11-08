using System.Text.Json.Serialization;
using Loris.Automations.Triggers.Example;

namespace Loris.Automations.Triggers;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ExampleTriggerDto), typeDiscriminator: ExampleTriggerDto.Discriminator)]
public interface ITriggerDto : IAutomationBlock { }
