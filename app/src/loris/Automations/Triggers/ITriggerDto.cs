using System.Text.Json.Serialization;
using Loris.Automations.Triggers.Example;
using Loris.Automations.Triggers.SensorCrossed;
using Loris.Automations.Triggers.Time;

namespace Loris.Automations.Triggers;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TimeTriggerDto), typeDiscriminator: TimeTriggerDto.Discriminator)]
[JsonDerivedType(typeof(ExampleTriggerDto), typeDiscriminator: ExampleTriggerDto.Discriminator)]
[JsonDerivedType(
    typeof(SensorCrossedTriggerDto),
    typeDiscriminator: SensorCrossedTriggerDto.Discriminator
)]
public interface ITriggerDto : IAutomationBlock
{
    [JsonIgnore]
    public new string ServiceKey { get; }
}
