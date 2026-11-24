using System.ComponentModel;
using Loris.Models;
using Loris.OpenApi;

namespace Loris.Dtos.Device;

public record DeviceReplaceRequest
{
    [JsonOneOfTypes(typeof(DevEui), typeof(string), typeof(Guid))]
    public required object With { get; init; }

    [DefaultValue(DataBehaviour.OVERLAP)]
    public required DataBehaviour ReadingsBehaviour { get; init; }
}

public enum DataBehaviour
{
    OVERLAP,
    DISCARD,
}
