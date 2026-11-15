using Loris.Data;
using Loris.Logging;
using Loris.Messages.Event;

namespace Loris.Channels;

public class EventChannelHandler(
    ILogger<EventChannelHandler> logger,
    IAutomationIndex index,
    IStateCache stateCache
) : IEventChannelHandler
{
    private readonly ILogger<EventChannelHandler> _logger = logger;
    private readonly IAutomationIndex _index = index;
    private readonly IStateCache _stateCache = stateCache;

    public async Task HandleAsync(IEventMessage message, CancellationToken stoppingToken)
    {
        _logger.EventReceived(message);

        if (!_index.TryGet(message.Event, out var automations))
            return;

        // filter automations
        var filtered = automations
            .Where(a =>
            {
                if (a.TriggerFilter.Invoke(message) == true)
                {
                    _logger.AutomationTriggered(a.Id, a.Name, message);
                    return true;
                }
                return false;
            })
            .Where(a =>
            {
                if (a.Conditions.All(conf => conf.Invoke(_stateCache)) == true)
                {
                    _logger.AutomationConditionsMet(a.Id, a.Name);
                    return true;
                }
                _logger.AutomationConditionsNotMet(a.Id, a.Name);
                return true;
            }); // filter by trigger

        foreach (var automation in filtered)
        {
            foreach (var action in automation.Actions)
            {
                // TODO: add static expression logging
                // and capture action context with exceptions and log these
                await action(stoppingToken);
            }
        }
    }
}
