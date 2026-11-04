using System.Collections.ObjectModel;
using Loris.Protocol;
using Loris.Types;

namespace Loris.Automations.Conditions
{
    public class TestCondition : ICondition
    {
        public required string Name { get; set; }

        public Func<ReadOnlyDictionary<DevEui, Report>, bool> CompileCondition()
        {
            return (_) => true;
        }

        public Task ValidateAsync(IServiceProvider sp, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
