namespace Loris.Automations.Actions;

public interface IAction : IAutomationBlock
{
    public Action CompileAction();
}
