using System.Collections.ObjectModel;
using Loris.Types;

namespace Loris.Automations;

public interface IAutomationBlockCompiler<TDto, TResult>
{
    public TResult Compile(TDto dto, LookupContext? lookup);

    public ValueTask<bool> TryValidateAsync(
        TDto dto,
        out LookupContext? lookup,
        CancellationToken cancellationToken = default
    );
}
