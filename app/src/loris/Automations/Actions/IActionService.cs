using System.Collections.ObjectModel;

namespace Loris.Automations.Actions;

public interface IActionService
{
    public Action CompileAction(IActionDto dto, ReadOnlyDictionary<string, object> lookup);

    public bool TryValidate(IActionDto dto, out ReadOnlyDictionary<string, object> lookup);
}
