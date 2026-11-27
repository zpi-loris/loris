using Loris.Automations.Helpers;
using Loris.Data;
using static Loris.Automations.Helpers.TimeValidation;

namespace Loris.Automations.Conditions.Time;

public class TimeConditionCompiler : ConditionCompilerBase<TimeConditionDto>
{
    private const string BEFORE = "Before";
    private const string AFTER = "After";

    protected override async Task<BlockValidationResult> ValidateAsync(
        TimeConditionDto dto,
        CancellationToken cancellationToken
    )
    {
        var errors = new List<BlockError>();

        if (dto.Before is null && dto.After is null)
        {
            return BlockValidationResult.Failure([
                new BlockError(
                    "{" + nameof(dto.Before) + "," + nameof(dto.After) + "}",
                    "",
                    $"You must specify at least one of: {nameof(dto.Before)} or {nameof(dto.After)}"
                ),
            ]);
        }

        var lookup = new Dictionary<string, object>();

        var items = new List<(object?, string)> { (dto.Before, BEFORE), (dto.After, AFTER) };
        foreach (var (item, name) in items)
        {
            switch (item)
            {
                case TimeSpan span:
                    ValidateTimeSpan(errors, lookup, name, span);
                    break;
                case TimeMap map:
                    ValidateTimeMap(errors, lookup, name, map);
                    break;
                default:
                    break;
            }
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        return lookup;
    }

    protected override Func<IStateCache, bool> Compile(TimeConditionDto dto, LookupContext? lookup)
    {
        lookup!.TryGet<TimeMap>(AFTER, out var timeAfter);
        lookup!.TryGet<TimeMap>(BEFORE, out var timeBefore);

        // The time creation must happen inside the functions, because
        // we want to defer it up until the condition invocation
        return (timeAfter, timeBefore) switch
        {
            (TimeMap, TimeMap) => (_) =>
                CheckBeforeAndAfter(
                    timeAfter.Hour * 60 + timeAfter.Minute,
                    timeBefore.Hour * 60 + timeBefore.Minute
                ),
            (TimeMap, null) => (_) => CheckAfter(timeAfter),
            (null, TimeMap) => (_) => CheckBefore(timeBefore),
            // this should not happend after validation, so it's better to log this
            (null, null) => throw new NotImplementedException(),
        };
    }

    // before: "06:00:00"
    // after: "22:00:00"

    private static bool CheckBeforeAndAfter(int after, int before)
    {
        // I think we need to use local time instead of UTC here
        var now = DateTime.Now;
        var minutes = now.Hour * 60 + now.Minute;

        // example (at night)
        // before: "06:00"
        // after: "22:00"
        if (after > before)
        {
            if (minutes >= before && minutes < after)
            {
                // example: "12:00"
                return false;
            }

            // at this point it must be in either range
            return true;
        }

        // example (at day)
        // before: "20:00"
        // after: "08:00"
        if (minutes >= after && minutes < before)
        {
            // example: "12:00"
            return true;
        }
        return false;
    }

    private static bool CheckBefore(TimeMap before)
    {
        var now = DateTime.Now;
        if (now.Hour < before.Hour)
        {
            return true;
        }
        if (now.Hour == before.Hour && now.Minute < before.Minute)
        {
            return true;
        }
        return false;
    }

    private static bool CheckAfter(TimeMap after)
    {
        var now = DateTime.Now;
        if (now.Hour > after.Hour)
        {
            return true;
        }
        if (now.Hour == after.Hour && now.Minute >= after.Minute)
        {
            return true;
        }
        return false;
    }
}
