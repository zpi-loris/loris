using System.Collections.ObjectModel;
using Loris.Automations.Conditions;
using Loris.Data;

namespace Loris.Automations.conditions;

public abstract class ConditionCompilerBase<TDto> : IConditionCompiler
    where TDto : IConditionDto
{
    public Func<StateCache, bool> Compile(
        IConditionDto dto,
        ReadOnlyDictionary<string, object> lookup
    )
    {
        var typedDto = EnsureTyped(dto, nameof(dto));
        return Compile(typedDto, lookup);
    }

    public ValueTask<bool> TryValidateAsync(
        IConditionDto dto,
        out ReadOnlyDictionary<string, object> lookup
    )
    {
        var typedDto = EnsureTyped(dto, nameof(dto));
        return TryValidateAsync(typedDto, out lookup);
    }

    protected abstract Func<StateCache, bool> Compile(
        TDto dto,
        ReadOnlyDictionary<string, object> lookup
    );

    protected abstract ValueTask<bool> TryValidateAsync(
        TDto dto,
        out ReadOnlyDictionary<string, object> lookup
    );

    private static TDto EnsureTyped(IConditionDto dto, string? paramName = null)
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
