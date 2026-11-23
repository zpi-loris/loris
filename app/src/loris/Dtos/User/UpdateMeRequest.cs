namespace Loris.Dtos.User;

public record UpdateMeRequest
{
    public string? Username { get; set; }
    public string? Password { get; set; }
}
