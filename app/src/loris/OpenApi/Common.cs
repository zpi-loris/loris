using Loris.Results;
using Microsoft.AspNetCore.Mvc;

namespace Loris.OpenApi;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ProducesUnauthenticatedAttribute
    : ProducesResponseTypeAttribute<ResultProblemDetails>
{
    public ProducesUnauthenticatedAttribute()
        : base(StatusCodes.Status401Unauthorized)
    {
        Description = "Unauthenticated";
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ProducesSuccessNoContentAttribute : ProducesResponseTypeAttribute
{
    public ProducesSuccessNoContentAttribute()
        : base(StatusCodes.Status204NoContent)
    {
        Description = "Success";
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ProducesSuccessAttribute : ProducesResponseTypeAttribute
{
    public ProducesSuccessAttribute()
        : base(StatusCodes.Status200OK)
    {
        Description = "Success";
    }
}
