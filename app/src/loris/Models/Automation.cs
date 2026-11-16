namespace Loris.Models;

public class Automation
{
    public Guid AutomationId { get; set; }

    public string Name { get; set; } = null!;

    public string Triggers { get; set; } = null!;

    public string Conditions { get; set; } = null!;

    public string Actions { get; set; } = null!;
}
