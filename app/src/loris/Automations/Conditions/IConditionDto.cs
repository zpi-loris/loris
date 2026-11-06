using System.Text.Json.Serialization;
using Loris.Automations.Conditions.TestInvalid;
using Loris.Automations.Conditions.TestValid;

namespace Loris.Automations.Conditions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(
    typeof(TestValidConditionDto),
    typeDiscriminator: TestValidConditionDto.Discriminator
)]
[JsonDerivedType(
    typeof(TestInvalidConditionDto),
    typeDiscriminator: TestInvalidConditionDto.Discriminator
)]
public interface IConditionDto
{
    [JsonIgnore]
    public string ServiceKey { get; }
}
