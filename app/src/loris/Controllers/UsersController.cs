using Loris.Controllers.Requests;
using Loris.Controllers.Responses;
using Loris.OpenApi;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesUnauthenticated]
[ProducesUnauthorized]
public class UsersController : ControllerBase
{
    /// <summary>
    /// Get all users
    /// </summary>
    /// <remarks>
    /// Fetches a list of all users in the system.
    /// This requires admin role
    /// </remarks>
    [HttpGet]
    [ProducesSuccess]
    public Task<IEnumerable<UserResponse>> GetUsers() => throw new NotImplementedException();

    /// <summary>
    /// Get user by ID
    /// </summary>
    /// <remarks>
    /// Fetches specific user identified by userId.
    /// This requires admin role
    /// </remarks>
    [HttpGet("{userId}")]
    [ProducesSuccess]
    [ProducesNotFound("user")]
    public Task<UserResponse> GetUser(Guid userId) => throw new NotImplementedException();

    /// <summary>
    /// Create new user
    /// </summary>
    /// <remarks>
    /// Creates a new user with specified role
    /// This requires admin role
    /// </remarks>
    [HttpPost]
    [ProducesUserCreated]
    [ProducesBadRequest]
    [ProducesUserConflict]
    public Task<UserResponse> CreateUser([FromBody] CreateUserRequest request) =>
        throw new NotImplementedException();

    /// <summary>
    /// Update user by ID
    /// </summary>
    /// <remarks>
    /// Updates the details of a specific user identified by userId.
    /// This requires admin role
    /// </remarks>
    [HttpPatch("{userId}")]
    [ProducesSuccess]
    [ProducesBadRequest]
    [ProducesUserConflict]
    [ProducesNotFound("user")]
    public Task<UserResponse> UpdateUser(Guid userId, [FromBody] UpdateUserRequest request) =>
        throw new NotImplementedException();

    /// <summary>
    /// Delete user by ID
    /// </summary>
    /// <remarks>
    /// Deletes a specific user identified by userId.
    /// This requires admin role
    /// </remarks>
    [HttpDelete("{userId}")]
    [ProducesSuccessNoContent]
    [ProducesNotFound("user")]
    public Task<IActionResult> DeleteUser(Guid userId) => throw new NotImplementedException();
}
