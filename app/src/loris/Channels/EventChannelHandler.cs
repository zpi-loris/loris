using Loris.Data;
using Loris.Messages.Event;

namespace Loris.Channels;

public class EventChannelHandler(
    ILogger<EventChannelHandler> logger,
    AutomationIndex index,
    StateCache stateCache
) : IEventChannelHandler
{
    private readonly ILogger<EventChannelHandler> _logger = logger;
    private readonly AutomationIndex _index = index;
    private readonly StateCache _stateCache = stateCache;

    public async Task HandleAsync(IEventMessage message, CancellationToken stoppingToken)
    {
        if (!_index.TryGet(message.Event, out var automations))
            return;

        // filter automations
        var filtered = automations
            .Where(a => a.TriggerFilter.Invoke(message)) // filter by trigger
            .Where(a => a.Conditions.All(conf => conf.Invoke(_stateCache))); // filter by conditions

        foreach (var automation in filtered)
        {
            _logger.LogDebug("Automation invoked by message"); // TODO: add static expression logging

            foreach (var action in automation.Actions)
            {
                await action(stoppingToken);
            }
        }
    }
}
