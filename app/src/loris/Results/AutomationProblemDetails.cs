using Microsoft.AspNetCore.Mvc;

namespace Loris.Results;

public class AutomationProblemDetails : ProblemDetails
{
    public IReadOnlyList<InvalidRuleGroup> InvalidRules { get; set; } = [];
}

// TODO: document the difference between this and BlockError
public sealed record AutomationFieldError(string Pointer, object? Value, string Details);

public sealed record InvalidRuleGroup(
    Guid Id,
    string Name,
    IReadOnlyList<AutomationFieldError> Errors
);
