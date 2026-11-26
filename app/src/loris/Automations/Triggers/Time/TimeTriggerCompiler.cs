using Loris.Automations.Helpers;
using Loris.Events;
using Loris.Messages.Event;
using static Loris.Automations.Helpers.TimeValidation;

namespace Loris.Automations.Triggers.Time;

public class TimeTriggerCompiler : TriggerCompilerBase<TimeTriggerDto>
{
    private const string TIME = "Time";

    protected override async Task<BlockValidationResult> ValidateAsync(
        TimeTriggerDto dto,
        CancellationToken cancellationToken
    )
    {
        var errors = new List<BlockError>();
        Dictionary<string, object> lookup = [];

        switch (dto.Time)
        {
            case TimeMap map:
                ValidateTimeMap(errors, lookup, TIME, map);
                break;
            case TimeSpan span:
                ValidateTimeSpan(errors, lookup, TIME, span);
                break;
            default:
                // this should not happen. If DTO field is properly parametrized
                // deserialization failed somehow
                throw new ArgumentException(
                    $"DTOs {dto.Time} value should be of type TimeMap or TimeSpan"
                );
        }
        if (errors.Count > 0)
        {
            return errors;
        }

        return lookup;
    }

    protected override (IEvent Event, Func<IEventMessage, bool> Filter) Compile(
        TimeTriggerDto dto,
        LookupContext? lookup
    )
    {
        // if compile runs, the TIME key MUST have a TimeMap value assigned
        var time = lookup!.Get<TimeMap>(TIME);

        // we don't need to filter IEventMessage, because TimeEvent is
        // self contained; All the information needes is inside TimeEvent
        return (TimeEvent.FromMap(time), (_) => true);
    }
}
