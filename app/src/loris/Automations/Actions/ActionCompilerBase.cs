namespace Loris.Automations.Actions;

public abstract class ActionCompilerBase<TDto> : IActionCompiler
    where TDto : IActionDto
{
    public Func<CancellationToken, Task> Compile(IActionDto dto, LookupContext? lookup)
    {
        var typedDto = EnsureTyped(dto, nameof(dto));
        return Compile(typedDto, lookup);
    }

    public Task<ValidationResult> ValidateAsync(
        IActionDto dto,
        CancellationToken cancellationToken = default
    )
    {
        var typedDto = EnsureTyped(dto, nameof(dto));
        return ValidateAsync(typedDto, cancellationToken);
    }

    protected abstract Func<CancellationToken, Task> Compile(TDto dto, LookupContext? lookup);

    protected abstract Task<ValidationResult> ValidateAsync(
        TDto dto,
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
