using System.Text.Json.Serialization;
using Loris.Automations.Triggers.Test;

namespace Loris.Automations.Triggers;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TestTriggerDto), typeDiscriminator: TestTriggerDto.Discriminator)]
public interface ITriggerDto
{
    [JsonIgnore]
    public string ServiceKey { get; }
}
