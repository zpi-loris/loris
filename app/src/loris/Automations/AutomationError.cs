namespace Loris.Automations;

public record AutomationError(
    string Id,
    string Name,
    string Pointer,
    string Value,
    string Details
) { }
