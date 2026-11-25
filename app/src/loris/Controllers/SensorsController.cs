using Loris.Dtos.Sensor;
using Loris.Extensions;
using Loris.OpenApi;
using loris.Services;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesUnauthenticated]
[ProducesSuccess]
public class SensorsController(ISensorService sensorService) : ControllerBase
{
    private readonly ISensorService _sensorService = sensorService;

    /// <summary>
    /// Get sensor readings by id
    /// </summary>
    /// <remarks>
    /// Retrieves sensor readings for a specific sensor identified by its unique ID.
    /// The readings can be filtered based on a specified time range
    /// using the 'from' and 'to' query parameters. The response will contain
    /// a collection of sensor readings within the specified time frame.
    /// </remarks>
    /// <param name="from">The start datetime for filtering sensor readings.</param>
    /// <param name="to">The end datetime for filtering sensor readings.</param>
    [HttpGet("{id}/readings")]
    [ProducesSensorNotFound]
    public async Task<ActionResult<IEnumerable<SensorReadingResponse>>> GetReadings(
        Guid id,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        CancellationToken ct
    ) => this.ToActionResult(await _sensorService.GetSensorReadings(id, from, to, ct));

    /// <summary>
    /// Gets averaged sensor reading by ID
    /// </summary>
    /// <remarks>
    /// Retrieves <b>numeric</b> readings for a specific sensor identified by its unique ID.
    /// The readings are grouped over specified time interval, and the average, min and max are returned.
    /// The readings can be filtered based on a specified time range
    /// using the 'from' and 'to' query parameters.
    /// </remarks>
    /// <param name="from">The start datetime for filtering sensor readings.</param>
    /// <param name="to">The end datetime for filtering sensor readings.</param>
    /// <param name="interval"> The time interval for grouping.</param>
    [HttpGet("{id}/readings-average")]
    [ProducesSensorNotFound]
    [ProducesSensorNotNumeric]
    public async Task<
        ActionResult<IEnumerable<SensorReadingAverageResponse>>
    > GetNumericReadingsAverage(
        Guid id,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] TimeSpan interval,
        CancellationToken ct
    ) =>
        this.ToActionResult(
            await _sensorService.GetSensorReadingsAverage(id, from, to, interval, ct)
        );
}
