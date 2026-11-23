using System.ComponentModel;
using Loris.Controllers.Requests;
using Loris.Controllers.Responses;
using Loris.Extensions;
using Loris.Models;
using Loris.OpenApi;
using Loris.Services;
using Microsoft.AspNetCore.Mvc;

namespace Loris.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[ProducesUnauthenticated]
[ProducesUnauthorized]
public class UsersController(IUserService service) : ControllerBase
{
    private readonly IUserService _service = service;

    /// <summary>
    /// Get all users
    /// </summary>
    /// <remarks>
    /// Fetches a list of all users in the system.
    /// This requires admin role
    /// </remarks>
    [HttpGet]
    [ProducesSuccess]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers(
        [DefaultValue(null)] [FromQuery] List<UserRole> role,
        CancellationToken ct
    ) => this.ToActionResult(await _service.ListUsersAsync(role, ct));

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
    public async Task<ActionResult<UserResponse>> GetUser(Guid userId, CancellationToken ct) =>
        this.ToActionResult(await _service.GetUserAsync(userId, ct));

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
    public async Task<ActionResult<UserResponse>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken ct
    ) => this.ToActionResult(await _service.CreateUserAsync(request, ct));

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
    public async Task<ActionResult<UserResponse>> UpdateUser(
        Guid userId,
        [FromBody] UpdateUserRequest request,
        CancellationToken ct
    ) => this.ToActionResult(await _service.UpdateUserAsync(request, ct));

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
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken ct) =>
        this.ToActionResult(await _service.DeleteUserAsync(userId, ct));
}
