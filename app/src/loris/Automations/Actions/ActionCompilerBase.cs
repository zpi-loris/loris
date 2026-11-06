using System.Collections.ObjectModel;
using Loris.Automations.Conditions;

namespace Loris.Automations.Actions;

public abstract class ActionCompilerBase<TDto> : IActionCompiler
    where TDto : IConditionDto
{
    public Action Compile(IActionDto dto, ReadOnlyDictionary<string, object> lookup)
    {
        var typedDto = EnsureTyped(dto, nameof(dto));
        return Compile(typedDto, lookup);
    }

    public ValueTask<bool> TryValidateAsync(
        IActionDto dto,
        out ReadOnlyDictionary<string, object> lookup
    )
    {
        var typedDto = EnsureTyped(dto, nameof(dto));
        return TryValidateAsync(typedDto, out lookup);
    }

    protected abstract Action Compile(TDto dto, ReadOnlyDictionary<string, object> lookup);

    protected abstract ValueTask<bool> TryValidateAsync(
        TDto dto,
        out ReadOnlyDictionary<string, object> lookup
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
