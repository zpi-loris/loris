using Loris.Protocol;
using Loris.Types;
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

    public Device Device { get; set; } = null!;
}
