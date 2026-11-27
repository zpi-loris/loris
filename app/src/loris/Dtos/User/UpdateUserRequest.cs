using Loris.Models;

namespace Loris.Dtos.User;

public record UpdateUserRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
    public UserRole? Role { get; set; }
}
