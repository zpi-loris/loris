using Loris.Results;
using Microsoft.AspNetCore.Mvc;

namespace Loris.OpenApi;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ProducesUserConflictAttribute
    : ProducesResponseTypeAttribute<ResultProblemDetails>
{
    public ProducesUserConflictAttribute()
        : base(StatusCodes.Status409Conflict)
    {
        Description = @"The given username is already taken. Username must be unique";
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ProducesUserCreatedAttribute : ProducesResponseTypeAttribute
{
    public ProducesUserCreatedAttribute()
        : base(StatusCodes.Status201Created)
    {
        Description = @"User created";
    }
}
