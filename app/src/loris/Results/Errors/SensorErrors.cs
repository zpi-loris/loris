namespace Loris.Results.Errors;

public static class SensorErrors
{
    public static Error NotFound(Guid sensorId) =>
        Error.NotFound("Sensors.NotFound", $"No sensor was found with the sensorId {sensorId}.");

    public static Error NotNumeric(Guid sensorId, string foundType) =>
        Error.NotFound(
            "Sensors.NotNumeric",
            $"Sensor {sensorId} is of {foundType} type. Must be numeric (float, int) instead."
        );
}
