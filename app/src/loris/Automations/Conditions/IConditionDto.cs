using System.Text.Json.Serialization;
using Loris.Automations.Conditions.Test;

namespace Loris.Automations.Conditions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TestConditionDto), typeDiscriminator: TestConditionDto.Discriminator)]
public interface IConditionDto
{
    [JsonIgnore]
    public string ServiceKey { get; }
}
