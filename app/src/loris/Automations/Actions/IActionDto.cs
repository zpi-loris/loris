using System.Text.Json.Serialization;
using Loris.Automations.Actions.TestInvalid;
using Loris.Automations.Actions.TestValid;

namespace Loris.Automations.Actions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TestValidActionDto), typeDiscriminator: TestValidActionDto.Discriminator)]
[JsonDerivedType(
    typeof(TestInvalidActionDto),
    typeDiscriminator: TestInvalidActionDto.Discriminator
)]
public interface IActionDto
{
    [JsonIgnore]
    public string ServiceKey { get; }
}
