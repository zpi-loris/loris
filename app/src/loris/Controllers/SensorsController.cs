using Loris.Controllers.Requests;
using Loris.OpenApi;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesUnauthenticated]
[ProducesSuccess]
public class SensorsController : ControllerBase
{
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
    public Task<IEnumerable<SensorReadingDto>> GetReadings(
        Guid id,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to
    ) => throw new NotImplementedException();
}
