using System.Text.Json.Serialization;
using Loris.Automations.Triggers.Example;
using Loris.Automations.Triggers.SensorCrossed;

namespace Loris.Automations.Triggers;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ExampleTriggerDto), typeDiscriminator: ExampleTriggerDto.Discriminator)]
[JsonDerivedType(
    typeof(SensorCrossedTriggerDto),
    typeDiscriminator: SensorCrossedTriggerDto.Discriminator
)]
public interface ITriggerDto : IAutomationBlock { }
