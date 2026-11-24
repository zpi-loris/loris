using System.ComponentModel;
using Loris.Json;
using Loris.Models;

namespace Loris.Dtos.Device;

public record DeviceReplaceRequest
{
    [JsonOneOfTypes(typeof(DevEui), typeof(Guid), typeof(string))]
    public required object With { get; init; }

    [DefaultValue(DataBehaviour.DISCARD)]
    public DataBehaviour ReadingsBehaviour { get; init; } = DataBehaviour.DISCARD;
}

public enum DataBehaviour
{
    OVERLAP,
    DISCARD,
}
