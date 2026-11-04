using System.Text.Json.Serialization;
using Loris.Automations.Triggers.Test;

namespace Loris.Automations.Triggers;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TestTriggerDto), typeDiscriminator: "test")]
public interface ITriggerDto { }
