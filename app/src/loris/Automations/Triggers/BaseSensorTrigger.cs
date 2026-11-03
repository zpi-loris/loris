using Loris.Messages;
using Microsoft.EntityFrameworkCore;

namespace Loris.Automations.Triggers;

public abstract class BaseSensorTrigger : IAutomationBlock
{
    public required string DeviceName { get; set; }
    public required string SensorName { get; set; }

    public abstract bool Validate(DbContext context);

    public abstract Func<IEventMessage, bool> CreateTriggerFilter();
}
