using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Loris.Automations;

public interface IAutomationBlock
{
    public Task ValidateAsync(IServiceProvider sp, CancellationToken ct);
}
