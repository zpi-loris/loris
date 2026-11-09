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

    public Device Device { get; set; } = null!;

    // TODO: UPDATE MODEL
    //
    // Enum column 'public.functions.arg_type' cannot be scaffolded, define a CLR enum type and add the property manually.
}
