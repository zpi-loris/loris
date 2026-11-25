using Loris.Models;

namespace Loris.Dtos.User;

public record UserResponse
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = null!;
    public UserRole Role { get; set; }
}
