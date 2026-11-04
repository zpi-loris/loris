using Loris.Messages;
using Microsoft.EntityFrameworkCore;

namespace Loris.Automations.Triggers;

public class SensorChangedTrigger : BaseSensorTrigger
{
    public required float Value { get; set; }

    public required bool TopBottom { get; set; }

    public override Func<IEventMessage, bool> CreateTriggerFilter()
    {
        return (_) => true;
    }

    public override bool Validate(DbContext context)
    {
        return true;
    }
}
