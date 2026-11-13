using Loris.Data;

namespace Loris.Automations.Conditions;

public abstract class ConditionCompilerBase<TDto> : IConditionCompiler
    where TDto : IConditionDto
{
    public Func<StateCache, bool> Compile(IConditionDto dto, LookupContext? lookup)
    {
        var typedDto = EnsureTyped(dto, nameof(dto));
        return Compile(typedDto, lookup);
    }

    public Task<ValidationResult> ValidateAsync(
        IConditionDto dto,
        CancellationToken cancellationToken = default
    )
    {
        var typedDto = EnsureTyped(dto, nameof(dto));
        return ValidateAsync(typedDto, cancellationToken);
    }

    protected abstract Func<StateCache, bool> Compile(TDto dto, LookupContext? lookup);

    protected abstract Task<ValidationResult> ValidateAsync(
        TDto dto,
        CancellationToken cancellationToken
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
