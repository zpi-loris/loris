using Loris.Models;

namespace Loris.Controllers.Responses;

public record UserResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = null!;
    public UserRole Role { get; set; }
}
