using System.Collections.ObjectModel;

namespace Loris.Automations;

public interface IAutomationBlockCompiler<TDto, TResult>
{
    public TResult Compile(TDto dto, ReadOnlyDictionary<string, object> lookup);

    public ValueTask<bool> TryValidateAsync(
        TDto dto,
        out ReadOnlyDictionary<string, object> lookup
    );
}
