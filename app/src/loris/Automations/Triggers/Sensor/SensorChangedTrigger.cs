namespace Loris.Automations.Triggers.Sensor;

public class SensorChangedTrigger : BaseSensorTrigger
{
    public required float Value { get; set; }

    public required bool Above { get; set; }
}
