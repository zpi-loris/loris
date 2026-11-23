using Loris.Dtos.User;
using Loris.Models;
using Loris.Results;

namespace Loris.Services;

public interface IUserService
{
    public Task<Result<IEnumerable<UserResponse>>> ListUsersAsync(
        List<UserRole> rolesFilterList,
        CancellationToken ct = default
    );
    public Task<Result<UserResponse>> GetUserAsync(Guid userId, CancellationToken ct = default);
    public Task<Result<UserResponse>> CreateUserAsync(
        CreateUserRequest requestDto,
        CancellationToken ct = default
    );
    public Task<Result<UserResponse>> UpdateUserAsync(
        Guid userId,
        UpdateUserRequest requestDto,
        CancellationToken ct = default
    );
    public Task<Result> DeleteUserAsync(Guid userId, CancellationToken ct = default);
}
