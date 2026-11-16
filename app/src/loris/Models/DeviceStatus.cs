using System.Text.Json.Serialization;

namespace Loris.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DeviceStatus
{
    PENDING,
    IGNORED,
    ACTIVE,
    DISCONNECTED,
}
