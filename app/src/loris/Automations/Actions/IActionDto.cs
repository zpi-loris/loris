using System.Text.Json.Serialization;
using Loris.Automations.Actions.DeviceCall;
using Loris.Automations.Actions.Example;

namespace Loris.Automations.Actions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(ExampleActionDto), typeDiscriminator: ExampleActionDto.Discriminator)]
[JsonDerivedType(typeof(DeviceCallActionDto), typeDiscriminator: DeviceCallActionDto.Discriminator)]
public interface IActionDto : IAutomationBlock { }
