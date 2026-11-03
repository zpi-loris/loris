using Loris.Messages;
using Microsoft.EntityFrameworkCore;

namespace Loris.Automations.Triggers;

public class SensorChangedTrigger : BaseSensorTrigger
{
    public required float Value { get; set; }

    public required bool TopBottom { get; set; }

    public override Func<IEventMessage, bool> CreateTriggerFilter()
    {
        throw new NotImplementedException();
    }

    public override bool Validate(DbContext context)
    {
        throw new NotImplementedException();
    }
}
