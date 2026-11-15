using Loris.Events;
using Loris.Messages.Event;
using Loris.Types;

namespace Loris.Automations.Triggers.Example;

public class ExamppleTriggerCompiler : TriggerCompilerBase<ExampleTriggerDto>
{
    // Validate is async, so you can ask database for validation or execute
    // other asynchronous operations
    //
    // You can add async keyword to the method, so it will create async state machine
    // and let you to return Task nested types
    protected override async Task<ValidationResult> ValidateAsync(
        ExampleTriggerDto dto,
        CancellationToken cancellationToken
    )
    {
        // initialize list of errors, that go to the validation Result
        var errors = new List<BlockError>();

        // do some asynchronous operation, like checking
        // in db for some names. Validation can be cancelled by the caller
        await Task.Delay(50, cancellationToken);

        HashSet<string> allowedNames = ["test", "example", "General Kenobi"];

        if (!allowedNames.Contains(dto.Name))
        {
            errors.Add(
                new(
                    nameof(dto.Name),
                    dto.Name,
                    $"Allowed values are: {string.Join(",", allowedNames)}"
                )
            );
        }

        if (dto.Value < 0)
        {
            errors.Add(new(nameof(dto.Value), dto.Value.ToString(), $"values must be >= 0"));
        }

        // asking for db validation will sometimes involve replacing
        // device name with DevEUI, oraz sensor/function name with its Id
        //
        // You can do this here,
        // - provide lookupContext object
        // - the Compile method will be provided with the very same instance you
        // just passed

        Dictionary<string, object> lookup = [];

        lookup.Add("Name", DevEui.Parse("efce217c394b45fe"));

        if (errors.Count > 0)
        {
            return ValidationResult.Failure(errors);
        }

        return ValidationResult.Success(lookup);

        // Or if there are no lookups, you don't have to initialize the dictionary
        // just return the overload;

        // return ValidationResult.Success();
    }

    protected override (IEvent Event, Func<IEventMessage, bool> Filter) Compile(
        ExampleTriggerDto dto,
        LookupContext? lookup
    )
    {
        // at this point, you the implementer
        // know if there is a lookup context provided,
        var devEui = lookup!.Get<DevEui>("Name");

        // create IEvent for fast automation index
        IEvent newEvent = new SensorEvent { DevEui = devEui, SensorID = 1 };

        // create triggerFilter delegate, to filter the IEventMessage
        Func<IEventMessage, bool> filter = (ival) =>
        {
            if (ival is SensorEventMessage eventMessage)
            {
                return eventMessage.Current.IntVal == dto.Value;
            }
            return false;
        };

        // return compiled
        return (Event: newEvent, Filter: filter);
    }
}
