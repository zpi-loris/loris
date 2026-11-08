using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Loris.Models;

[Table("functions")]
[Index("FunctionId", "DeviceId", Name = "functions_unique_0", IsUnique = true)]
public partial class Function
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("function_id")]
    public int FunctionId { get; set; }

    [Column("device_id")]
    public Guid DeviceId { get; set; }

    [Column("name")]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string Description { get; set; } = null!;

    [ForeignKey("DeviceId")]
    [InverseProperty("Functions")]
    public virtual Device Device { get; set; } = null!;

    // TODO: UPDATE MODEL
    //
    // Enum column 'public.functions.arg_type' cannot be scaffolded, define a CLR enum type and add the property manually.
}
