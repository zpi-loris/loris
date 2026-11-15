using Microsoft.AspNetCore.Mvc;

namespace Loris.Results;

public class AutomationProblemDetails : ProblemDetails
{
    public IReadOnlyList<InvalidRuleGroup> InvalidRules { get; set; } = [];
}

public sealed record AutomationFieldError(string Pointer, object? Value, string Details);

public sealed record InvalidRuleGroup(
    Guid Id,
    string Name,
    IReadOnlyList<AutomationFieldError> Errors
);
