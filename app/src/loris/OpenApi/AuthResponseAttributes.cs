using Loris.Results;
using Microsoft.AspNetCore.Mvc;

namespace Loris.OpenApi;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ProducesInvalidCredentials : ProducesResponseTypeAttribute<ResultProblemDetails>
{
    public ProducesInvalidCredentials()
        : base(StatusCodes.Status401Unauthorized)
    {
        Description = "Invalid credentials";
    }
}
