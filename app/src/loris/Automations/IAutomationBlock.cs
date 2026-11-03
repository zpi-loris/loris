using Microsoft.EntityFrameworkCore;

namespace Loris.Automations;

public interface IAutomationBlock
{
    public bool Validate(DbContext context);
}
