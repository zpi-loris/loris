using Loris.Results;
using Microsoft.AspNetCore.Mvc;

namespace Loris.OpenApi;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ProducesSensorNotFoundAttribute
    : ProducesResponseTypeAttribute<ResultProblemDetails>
{
    public ProducesSensorNotFoundAttribute()
        : base(StatusCodes.Status404NotFound)
    {
        Description = "The sensor was not found";
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ProducesSensorNotNumericAttribute
    : ProducesResponseTypeAttribute<ResultProblemDetails>
{
    public ProducesSensorNotNumericAttribute()
        : base(StatusCodes.Status400BadRequest)
    {
        Description = "The sensor must be numeric (float or int)";
    }
}
