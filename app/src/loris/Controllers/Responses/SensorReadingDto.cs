using Loris.OpenApi;

namespace Loris.Controllers.Requests;

public class SensorReadingDto
{
    public DateTime Time { get; set; }

    [OneOfTypes(typeof(bool), typeof(int), typeof(float), typeof(string))]
    public object Value { get; set; }
}
