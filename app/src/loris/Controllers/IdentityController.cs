using Loris.Dtos.User;
using Loris.OpenApi;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[ApiController]
[Route("api/me")]
[Produces("application/json")]
[ProducesUnauthenticated]
[ProducesSuccess]
public class IdentityController : ControllerBase
{
    /// <summary>
    /// Get current user
    /// </summary>
    /// <remarks>
    /// Retrieves the details of the currently authenticated user.
    /// </remarks>
    [HttpGet]
    public Task<UserResponse> GetMe() => throw new NotImplementedException();

    /// <summary>
    /// Update current user
    /// </summary>
    /// <remarks>
    /// Updates the details of the currently authenticated user.
    /// </remarks>
    [HttpPatch]
    [ProducesBadRequest]
    [ProducesUserConflict]
    public Task<UserResponse> Update([FromBody] UpdateMeRequest request) =>
        throw new NotImplementedException();
}
