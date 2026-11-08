using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Loris.Models;

[Table("devices")]
[Index("Deveui", Name = "devices_deveui_key", IsUnique = true)]
[Index("FriendlyName", Name = "devices_friendly_name_key", IsUnique = true)]
public partial class Device
{
    [Key]
    [Column("device_id")]
    public Guid DeviceId { get; set; }

    [Column("deveui")]
    public byte[] Deveui { get; set; } = null!;

    [Column("device_name")]
    public string DeviceName { get; set; } = null!;

    [Column("friendly_name")]
    public string FriendlyName { get; set; } = null!;

    [InverseProperty("Device")]
    public virtual ICollection<Function> Functions { get; set; } = [];

    [InverseProperty("Device")]
    public virtual ICollection<Sensor> Sensors { get; set; } = [];
}
