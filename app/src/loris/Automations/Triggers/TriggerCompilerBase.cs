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

    public ValueTask<bool> TryValidateAsync(
        ITriggerDto dto,
        out LookupContext? lookup,
        CancellationToken cancellationToken = default
    )
    {
        var typedDto = EnsureTyped(dto, nameof(dto));
        return TryValidateAsync(typedDto, out lookup, cancellationToken);
    }

    protected abstract (IEvent Event, Func<IEventMessage, bool> Filter) Compile(
        TDto dto,
        LookupContext? lookup
    );

    protected abstract ValueTask<bool> TryValidateAsync(
        TDto dto,
        out LookupContext? lookup,
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
