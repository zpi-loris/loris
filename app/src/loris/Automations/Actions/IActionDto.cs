using System.Text.Json.Serialization;
using Loris.Automations.Actions.Example;

namespace Loris.Automations.Actions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ExampleActionDto), typeDiscriminator: ExampleActionDto.Discriminator)]
public interface IActionDto : IAutomationBlock { }
