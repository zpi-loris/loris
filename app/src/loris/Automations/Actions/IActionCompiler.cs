namespace Loris.Automations.Actions;

public interface IActionCompiler
    : IAutomationBlockCompiler<IActionDto, Func<CancellationToken, Task>> { }
