using System.Text.Json;
using Loris.Automations;
using Loris.Automations.Actions;
using Loris.Automations.Conditions;
using Loris.Automations.Triggers;
using Loris.Data;
using Loris.Models;
using Loris.Services;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Loris.Jobs;

public class PreStartupAutomationJob(
    ILogger<PreStartupAutomationJob> logger,
    IServiceScopeFactory serviceScopeFactory,
    IOptions<JsonOptions> jsonOptions
) : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory = serviceScopeFactory;
    private readonly ILogger<PreStartupAutomationJob> _logger = logger;
    private readonly JsonSerializerOptions _jsonOptions = jsonOptions.Value.SerializerOptions;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{Job} started", nameof(PreStartupStateCacheJob));

        await using var scope = _scopeFactory.CreateAsyncScope();

        var ctx = scope.ServiceProvider.GetRequiredService<LorisDbContext>();
        var service = scope.ServiceProvider.GetRequiredService<IAutomationService>();

        var automations = await ctx.Automations.ToListAsync(cancellationToken);

        var dtos = TransformToDtos(automations);

        // we want to force the application of the rules, and log the errors if any
        var errors = await service.ApplyAsync(dtos, force: true, cancellationToken);
        if (errors.Count > 0)
        {
            _logger.LogCritical(
                "{Count} Automations not applied due to errors: {Errors}",
                errors.GroupBy(e => e.Id).Count(),
                errors
            );
        }

        _logger.LogInformation("{Job} finished", nameof(PreStartupStateCacheJob));
    }

    private List<AutomationDto> TransformToDtos(IEnumerable<Automation> automations)
    {
        var list = new List<AutomationDto>(automations.Count());
        foreach (var automation in automations)
        {
            try
            {
                list.Add(ToDto(automation));
            }
            catch (Exception ex)
            {
                _logger.LogCritical(
                    ex,
                    "Automation {Id} failed deserialization",
                    automation.AutomationId
                );
            }
        }
        return list;
    }

    private AutomationDto ToDto(Automation automation)
    {
        return new AutomationDto(
            automation.AutomationId,
            automation.Name,
            JsonSerializer.Deserialize<ITriggerDto[]>(automation.Triggers, _jsonOptions)!,
            JsonSerializer.Deserialize<IConditionDto[]>(automation.Conditions, _jsonOptions)!,
            JsonSerializer.Deserialize<IActionDto[]>(automation.Actions, _jsonOptions)!
        );
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
