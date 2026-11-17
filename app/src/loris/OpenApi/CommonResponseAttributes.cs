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
public sealed class ProducesUnauthorizedAttribute
    : ProducesResponseTypeAttribute<ResultProblemDetails>
{
    public ProducesUnauthorizedAttribute()
        : base(StatusCodes.Status403Forbidden)
    {
        Description = "Not authorized to request";
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ProducesNotFoundAttribute : ProducesResponseTypeAttribute<ResultProblemDetails>
{
    public ProducesNotFoundAttribute(string entity)
        : base(StatusCodes.Status404NotFound)
    {
        Description = $"The {entity} was not found";
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

public sealed class ProducesBadRequestAttribute
    : ProducesResponseTypeAttribute<ResultProblemDetails>
{
    public ProducesBadRequestAttribute()
        : base(StatusCodes.Status400BadRequest)
    {
        Description = "Invalid request";
    }
}
