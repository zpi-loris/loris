using System.Text.Json.Serialization;
using Loris.Events;
using Loris.Messages;

namespace Loris.Automations.Triggers;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(SensorChangedTrigger), typeDiscriminator: "sensor-changed")]
[JsonDerivedType(typeof(SensorCrossedTrigger), typeDiscriminator: "sensor-crossed")]
[JsonDerivedType(typeof(TestTrigger), typeDiscriminator: "test")]
public interface ITrigger : IAutomationBlock
{
    public (IEvent Event, Func<IEventMessage, bool> Filter) CompileTrigger();
}
