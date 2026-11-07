using Loris.Events;
using Loris.Messages;

namespace Loris.Automations.Triggers;

public interface ITriggerCompiler
    : IAutomationBlockCompiler<ITriggerDto, (IEvent Event, Func<IEventMessage, bool> Filter)> { }
