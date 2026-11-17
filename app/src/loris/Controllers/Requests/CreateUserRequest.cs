using Loris.Models;

namespace Loris.Controllers.Requests;

public record CreateUserRequest
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public required UserRole Role { get; set; }
}
