using Loris.OpenApi;

namespace Loris.Dtos.Sensor;

public record SensorReadingResponse()
{
    public DateTime Time { get; set; }

    [JsonOneOfTypes(typeof(bool), typeof(int), typeof(float), typeof(string))]
    public object Value { get; set; } = null!;
}
