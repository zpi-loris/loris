using Loris.Models.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Loris.Models;

[Comment("Web client users")]
public partial class User
{
    public Guid UserId { get; set; }

    public string DisplayName { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;
}
