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
