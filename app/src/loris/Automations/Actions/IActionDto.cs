using System.Text.Json.Serialization;
using Loris.Automations.Actions.Test;

namespace Loris.Automations.Actions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TestActionDto), typeDiscriminator: TestActionDto.Discriminator)]
public interface IActionDto
{
    [JsonIgnore]
    public string ServiceKey { get; }
}
