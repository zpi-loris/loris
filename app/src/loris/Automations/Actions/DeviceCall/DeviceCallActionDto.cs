using System.Text.Json.Serialization;
using Loris.Json;

namespace Loris.Automations.Actions.DeviceCall;

// TODO make value optional
public record DeviceCallActionDto : IActionDto
{
    public required string Device { get; init; }
    public required string Function { get; init; }

    [JsonOneOfTypes(typeof(bool), typeof(int), typeof(float), typeof(string))]
    public object? Value { get; init; }

    [JsonPropertyName("wake-after")]
    public int? WakeAfter { get; init; }

    [JsonIgnore]
    public string ServiceKey => Discriminator;
    public const string Discriminator = "device-call";
}
