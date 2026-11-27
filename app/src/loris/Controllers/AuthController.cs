using Loris.OpenApi;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[ApiController]
[Route("auth/")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    /// <summary>
    /// Login user
    /// </summary>
    /// <remarks>
    /// Authenticates a user and returns a JWT token if the credentials are valid.
    /// </remarks>
    [HttpPost]
    [ProducesSuccess]
    [ProducesBadRequest]
    [ProducesInvalidCredentials]
    public Task<IActionResult> Login() => throw new NotImplementedException();
}
