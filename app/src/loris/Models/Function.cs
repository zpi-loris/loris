using System.Text.Json.Serialization;
using Loris.Protocol;
using Microsoft.EntityFrameworkCore;

namespace Loris.Models;

[Comment("Devices functions exposed via capabilities ")]
public partial class Function
{
    /// <summary>
    /// Artificial primary key
    /// </summary>
    public Guid FunctionId { get; set; }

    /// <summary>
    /// Locally unique ID in a single device
    /// </summary>
    public int DeviceFunctionId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public FuncArgType ArgType { get; set; }

    public Guid DeviceId { get; set; }

    //
    // REFERENCES
    //

    [JsonIgnore]
    public Device Device { get; set; } = null!;

    //
    // Proto helpers
    //

    public static Function FromProto(Guid deviceId, Protocol.Function function)
    {
        return new Function()
        {
            DeviceId = deviceId,
            DeviceFunctionId = function.Id,
            Name = function.Name,
            Description = function.Description,
            ArgType = function.ArgType,
        };
    }
}
