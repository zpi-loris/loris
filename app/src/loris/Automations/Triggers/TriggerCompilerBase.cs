using System.Collections.ObjectModel;
using Loris.Events;
using Loris.Messages;

namespace Loris.Automations.Triggers;

public abstract class TriggerCompilerBase<TDto> : ITriggerService
    where TDto : ITriggerDto
{
    public (IEvent Event, Func<IEventMessage, bool> Filter) Compile(
        ITriggerDto dto,
        ReadOnlyDictionary<string, object> lookup
    )
    {
        var typedDto = EnsureTyped(dto, nameof(dto));
        return Compile(typedDto, lookup);
    }

    public ValueTask<bool> TryValidateAsync(
        ITriggerDto dto,
        out ReadOnlyDictionary<string, object> lookup
    )
    {
        var typedDto = EnsureTyped(dto, nameof(dto));
        return TryValidateAsync(typedDto, out lookup);
    }

    protected abstract (IEvent Event, Func<IEventMessage, bool> Filter) Compile(
        TDto dto,
        ReadOnlyDictionary<string, object> lookup
    );

    protected abstract ValueTask<bool> TryValidateAsync(
        TDto dto,
        out ReadOnlyDictionary<string, object> lookup
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
