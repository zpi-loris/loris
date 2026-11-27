namespace Loris.Results.Errors;

public static class AutomationErrors
{
    public static Error NotFound(Guid id) =>
        Error.NotFound("Automations.NotFound", $"No automation was found with id {id}.");
}
