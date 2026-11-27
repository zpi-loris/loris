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

    //
    // REFERENCES
    //

    public ICollection<Function> Functions { get; } = [];

    public ICollection<Sensor> Sensors { get; } = [];

    public bool EqualsWithCapabilities(Device other)
    {
        if (Functions.Count != other.Functions.Count || Sensors.Count != other.Sensors.Count)
        {
            return false;
        }

        var orderedSensors = Sensors.OrderBy(s => s.DeviceSensorId);
        var otherOrderedSensors = other.Sensors.OrderBy(s => s.DeviceSensorId);

        foreach (var (thisSensor, otherSensor) in orderedSensors.Zip(otherOrderedSensors))
        {
            if (!thisSensor.EqualsWithDefinition(otherSensor))
            {
                return false;
            }
        }

        var orderedFunctions = Functions.OrderBy(f => f.DeviceFunctionId);
        var otherOrderedFunctions = other.Functions.OrderBy(f => f.DeviceFunctionId);

        foreach (var (thisFunction, otherFunction) in orderedFunctions.Zip(otherOrderedFunctions))
        {
            if (!thisFunction.EqualsWithDefinition(otherFunction))
            {
                return false;
            }
        }

        return true;
    }
}
