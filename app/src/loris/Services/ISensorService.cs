using Loris.Dtos.Sensor;
using Loris.Results;

namespace loris.Services;

public interface ISensorService
{
    public Task<Result<IEnumerable<SensorReadingResponse>>> GetSensorReadings(
        Guid sensorId,
        DateTime from,
        DateTime to,
        CancellationToken ct = default
    );

    public Task<Result<IEnumerable<SensorReadingAverageResponse>>> GetSensorReadingsAverage(
        Guid sensorId,
        DateTime from,
        DateTime to,
        TimeSpan timeInterval,
        CancellationToken ct = default
    );
}
