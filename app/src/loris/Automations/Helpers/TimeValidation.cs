namespace Loris.Automations.Helpers;

public static class TimeValidation
{
    /// <summary>
    /// Validates TimeMap to ensure its fields are in the allowed clock range
    /// </summary>
    /// <remarks>
    /// On success it assigns a valid map to the lookup using fieldName key<br/>
    /// On failure it assigns a each of the errors to the errors list
    /// </remarks>
    public static void ValidateTimeMap(
        List<BlockError> errors,
        Dictionary<string, object> lookup,
        string fieldName,
        TimeMap map
    )
    {
        var valid = true;
        if (map.Hour > 23 || map.Hour < 0)
        {
            errors.Add(
                new BlockError(fieldName, map.Hour.ToString(), "Hour must be in range 0-23")
            );
            valid = false;
        }
        if (map.Minute > 59 || map.Hour < 0)
        {
            errors.Add(
                new BlockError(fieldName, map.Minute.ToString(), "Minute must be in range 0-59")
            );
            valid = false;
        }
        if (valid)
            lookup.Add(fieldName, map);
    }

    /// <summary>
    /// Validates TimeSpan to ensure it consists only of HH:MM format.
    /// </summary>
    /// <remarks>
    /// On success it assigns a valid span to the lookup using fieldName key and <b>TimeMap</b> as a value<br/>
    /// On failure it assigns an error to the errors list
    /// </remarks>
    public static void ValidateTimeSpan(
        List<BlockError> errors,
        Dictionary<string, object> lookup,
        string fieldName,
        TimeSpan span
    )
    {
        if (span.Days > 0)
        {
            errors.Add(
                new BlockError(fieldName, span.ToString(), "Inline format allows only xx:xx")
            );
        }
        else
            lookup.Add(fieldName, new TimeMap(span.Hours, span.Minutes));
    }
}
