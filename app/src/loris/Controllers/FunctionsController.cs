using Loris.Dtos.Function;
using Loris.Extensions;
using Loris.OpenApi;
using Loris.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Loris.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesUnauthenticated]
[ProducesSensorNotFound]
public class FunctionsController(IFunctionService functionService) : ControllerBase
{
    private readonly IFunctionService _functionService = functionService;

    /// <summary>
    /// Invoke device function by id
    /// </summary>
    /// <param name="id">The ID of the function to invoke.</param>
    /// <param name="request">The request body containing the parameters for the function.</param>
    /// <remarks>
    /// Invokes a device function by providing its unique identifier
    /// and optional function argument in the request body. The request is validated against
    /// the function's expected parameters, and if successful, the downlink call is scheduled.
    /// The successful responnse is not a confirmation of the function execution on the device,
    /// but rather a confirmation that the request has been accepted and scheduled for execution.
    /// The time of the actual function execution on the device may vary based on the device's uplinks frequency.
    /// </remarks>
    [HttpPost("{id}/invoke")]
    [ProducesUnauthorized]
    [ProducesSuccessNoContent]
    public async Task<IActionResult> CallFunction(
        Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CallFunctionRequest request,
        CancellationToken ct
    ) => this.ToActionResult(await _functionService.CallFunction(id, request.Arg, ct));
}
