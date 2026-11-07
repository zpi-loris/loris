using System.Collections.ObjectModel;
using Loris.Automations.Conditions;
using Loris.Types;

namespace Loris.Automations.Actions;

public abstract class ActionCompilerBase<TDto> : IActionCompiler
    where TDto : IActionDto
{
    public Action Compile(IActionDto dto, LookupContext? lookup)
    {
        var typedDto = EnsureTyped(dto, nameof(dto));
        return Compile(typedDto, lookup);
    }

    public ValueTask<bool> TryValidateAsync(
        IActionDto dto,
        out LookupContext? lookup,
        CancellationToken cancellationToken = default
    )
    {
        var typedDto = EnsureTyped(dto, nameof(dto));
        return TryValidateAsync(typedDto, out lookup, cancellationToken);
    }

    protected abstract Action Compile(TDto dto, LookupContext? lookup);

    protected abstract ValueTask<bool> TryValidateAsync(
        TDto dto,
        out LookupContext? lookup,
        CancellationToken cancellationToken
    );

    private static TDto EnsureTyped(IActionDto dto, string? paramName = null)
    {
        if (dto is not TDto typedDto)
        {
            throw new ArgumentException(
                $"Invalid DTO type. Expected {typeof(TDto).Name}, got {dto.GetType().Name}.",
                paramName
            );
        }

        return typedDto;
    }
}
