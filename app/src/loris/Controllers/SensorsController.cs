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
    [HttpGet("{id}/readings")]
    [ProducesSensorNotFound]
    public Task<IEnumerable<SensorReadingDto>> GetReadings(
        Guid id,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to
    ) => throw new NotImplementedException();
}
