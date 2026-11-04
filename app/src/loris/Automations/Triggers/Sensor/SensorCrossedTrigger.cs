namespace Loris.Automations.Triggers.Sensor;

public class SensorCrossedTrigger : BaseSensorTrigger
{
    public required float Value { get; set; }

    public required bool TopBottom { get; set; }
}
