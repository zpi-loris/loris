using Loris.Events;
using Loris.Messages;
using Loris.Types;

namespace Loris.Automations.Triggers;

public abstract class TriggerCompilerBase<TDto> : ITriggerService
    where TDto : ITriggerDto
{
    public (IEvent Event, Func<IEventMessage, bool> Filter) Compile(
        ITriggerDto dto,
        LookupContext? lookup
    )
    {
        var typedDto = EnsureTyped(dto, nameof(dto));
        return Compile(typedDto, lookup);
    }

    public Task<ValidationResult> ValidateAsync(
        ITriggerDto dto,
        CancellationToken cancellationToken = default
    )
    {
        var typedDto = EnsureTyped(dto, nameof(dto));
        return ValidateAsync(typedDto, cancellationToken);
    }

    protected abstract (IEvent Event, Func<IEventMessage, bool> Filter) Compile(
        TDto dto,
        LookupContext? lookup
    );

    protected abstract Task<ValidationResult> ValidateAsync(
        TDto dto,
        CancellationToken cancellationToken
    );

    private static TDto EnsureTyped(ITriggerDto dto, string? paramName = null)
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
