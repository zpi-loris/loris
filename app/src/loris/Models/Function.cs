using System.Text.Json.Serialization;
using Loris.Protocol;
using Microsoft.EntityFrameworkCore;

namespace Loris.Models;

[Comment("Devices functions exposed via capabilities ")]
public partial class Function
{
    public DevEui DevEui { get; set; }

    public int FunctionId { get; set; }

    public string Name { get; set; } = null!;

    public string Description { get; set; } = null!;

    public FuncArgType ArgType { get; set; }

    //
    // REFERENCES
    //

    [JsonIgnore]
    public Device Device { get; set; } = null!;

    //
    // Proto helpers
    //

    public static Function FromProto(DevEui devEui, Protocol.Function function)
    {
        return new Function()
        {
            DevEui = devEui,
            FunctionId = function.Id,
            Name = function.Name,
            Description = function.Description,
            ArgType = function.ArgType,
        };
    }
}
