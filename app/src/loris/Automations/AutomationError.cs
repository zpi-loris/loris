namespace Loris.Automations;

public record AutomationError(
    Guid Id,
    string Name,
    string Pointer,
    string Value,
    string Details
) { }
