using Loris.Automations.Helpers;

namespace Loris.Events;

/// <summary>
/// TimeEvent is an event representing a specific time of day,
/// identified by its hour and minute.
/// </summary>
public readonly record struct TimeEvent(int Hour, int Minute) : IEvent<TimeEvent>
{
    public static TimeEvent FromMap(TimeMap map) => new(map.Hour, map.Minute);
}
