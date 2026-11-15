using Microsoft.AspNetCore.Mvc;

namespace Loris.Results;

public class AutomationProblemDetails : ProblemDetails
{
    public IReadOnlyList<InvalidRule> InvalidRules { get; set; } = [];

    public AutomationProblemDetails(ProblemDetails source)
    {
        Type = source.Type;
        Title = source.Title;
        Status = source.Status;
        Detail = source.Detail;
        Instance = source.Instance;

        foreach (var (key, value) in source.Extensions)
        {
            Extensions[key] = value;
        }
    }
}

public sealed record InvalidRule(
    Guid Id,
    string Name,
    IReadOnlyList<AutomationFieldError>? Errors = default
);

public sealed record AutomationFieldError(string Pointer, object? Value, string Details);
