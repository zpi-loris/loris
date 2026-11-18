using Loris.Results;
using Microsoft.AspNetCore.Mvc;

namespace Loris.OpenApi;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class ProducesAutomationValidationError
    : ProducesResponseTypeAttribute<AutomationProblemDetails>
{
    public ProducesAutomationValidationError()
        : base(StatusCodes.Status400BadRequest)
    {
        Description = "Automation rules are invalid. Compiler validations failed";
    }
}
