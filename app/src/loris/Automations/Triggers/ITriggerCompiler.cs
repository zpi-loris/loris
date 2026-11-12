using Loris.Events;
using Loris.Messages.Event;

namespace Loris.Automations.Triggers;

public interface ITriggerCompiler
    : IAutomationBlockCompiler<ITriggerDto, (IEvent Event, Func<IEventMessage, bool> Filter)> { }
