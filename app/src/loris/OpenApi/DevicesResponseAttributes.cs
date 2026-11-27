using Loris.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace Loris.OpenApi;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ProducesDeviceNotFoundAttribute
    : ProducesResponseTypeAttribute<ResultProblemDetails>
{
    public ProducesDeviceNotFoundAttribute()
        : base(StatusCodes.Status404NotFound)
    {
        Description = "The device was not found";
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ProducesDeviceStateConflictAttribute
    : ProducesResponseTypeAttribute<ResultProblemDetails>
{
    public ProducesDeviceStateConflictAttribute()
        : base(StatusCodes.Status409Conflict)
    {
        Description =
            @"The device is in a state that does not allow the requested operation.
            This involves accept, disconnect and ignore operations.
            The device must be in a state that allows the requested operation.";
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ProducesDeviceNameConflictAttribute
    : ProducesResponseTypeAttribute<ResultProblemDetails>
{
    public ProducesDeviceNameConflictAttribute()
        : base(StatusCodes.Status409Conflict)
    {
        Description =
            @"The device friendly name already exists 
            and/or is used by another device.";
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ProducesDeviceReplaceConflictAttribute
    : ProducesResponseTypeAttribute<ResultProblemDetails>
{
    public ProducesDeviceReplaceConflictAttribute()
        : base(StatusCodes.Status409Conflict)
    {
        Description =
            @"The substitute device is mismatched, or in a wrong
            state to proceed with the replacement";
    }
}

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class ProducesLoraWanGatewayErrorsAttribute : Attribute, IActionModelConvention
{
    public void Apply(ActionModel action)
    {
        action.Filters.Add(
            new ProducesResponseTypeAttribute<ResultProblemDetails>(StatusCodes.Status502BadGateway)
            {
                Description =
                    "The LoraWAN server returned an error when trying to perform the requested operation",
            }
        );
        action.Filters.Add(
            new ProducesResponseTypeAttribute<ResultProblemDetails>(
                StatusCodes.Status504GatewayTimeout
            )
            {
                Description =
                    "The LoraWAN server did not respond in time when trying to perform the requested operation",
            }
        );
    }
}
