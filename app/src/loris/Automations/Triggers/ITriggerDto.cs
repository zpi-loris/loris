using System.Text.Json.Serialization;
using Loris.Automations.Triggers.TestInvalid;
using Loris.Automations.Triggers.TestValid;

namespace Loris.Automations.Triggers;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TestValidTriggerDto), typeDiscriminator: TestValidTriggerDto.Discriminator)]
[JsonDerivedType(
    typeof(TestInvalidTriggerDto),
    typeDiscriminator: TestInvalidTriggerDto.Discriminator
)]
public interface ITriggerDto : IAutomationBlock { }
