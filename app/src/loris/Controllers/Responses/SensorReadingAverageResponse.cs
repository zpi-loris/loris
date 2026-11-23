using Loris.OpenApi;

namespace Loris.Controllers.Responses;

public record SensorReadingAverageResponse()
{
    public DateTime Time { get; set; }

    [JsonOneOfTypes(typeof(int), typeof(float))]
    public object Average { get; set; } = null!;

    [JsonOneOfTypes(typeof(int), typeof(float))]
    public object Min { get; set; } = null!;

    [JsonOneOfTypes(typeof(int), typeof(float))]
    public object Max { get; set; } = null!;

    public int Count { get; set; }
}
