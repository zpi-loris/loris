using Loris.Types;
using Microsoft.EntityFrameworkCore;

namespace Loris.Models;

[Comment("All of the devices from Chirpstack")]
public partial class Device
{
    public Guid DeviceId { get; set; }

    public DevEui DevEui { get; set; }

    public string DeviceName { get; set; } = null!;

    public string FriendlyName { get; set; } = null!;

    public DateTime LastSeen { get; set; }

    public DeviceStatus DeviceStatus { get; set; }

    public ICollection<Function> Functions { get; } = [];

    public ICollection<Sensor> Sensors { get; } = [];
}
