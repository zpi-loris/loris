using System.ComponentModel;
using Loris.Controllers.Requests;
using Loris.Models;
using Loris.OpenApi;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Loris.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesUnauthenticated]
[ProducesSensorNotFound]
public class FunctionsController : ControllerBase
{
    [HttpPost("{id}/invoke")]
    [ProducesUnauthorized]
    [ProducesSuccessNoContent]
    public Task<IActionResult> CallFunction(
        Guid id,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] CallFunctionRequest request,
        CancellationToken ct
    ) => throw new NotImplementedException();
}
