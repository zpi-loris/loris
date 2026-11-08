# Automation Engine

## Blocks

There are 3 blocks that build proper automation:

1. Triggers
2. Conditions
3. Actions

They are independant from each other, meaning that the implementations focus on
different dillemas.

### Triggers

They are responsible for triggering the automation to start running. Triggers
are composed from two objects:

- `IEvent` event key
- `Func<IEventMessage, bool>` filtering delegate

Event key is meant to be value type comparable, so it can be used as an
index/dictionary lookup key. For events see `Loris/Events` namespace (There are
already some defined).

Filtering delegate is meant to further filter the `IEventMessages` comming on
event channel. It's main role is to allow for declaring a logic, so the
automations trigger can analyze particular Event message metadata, that was not
possible to predict for `IEvent` to contain.

> _example_
>
> Consider trigger, that is listening for device sensor crossing value 5. The
> invariants we can put directly to `IEvent` are device and sensor ids. But the
> value check must be composed logically, so we can handle all the possible
> situations.

### Conditions

Conditions are additional checks, than can be used to create more sophisticated
automations. Users can declare, that triggered automations can only run, if all
of the conditions are valid like:

- time is _x_
- day is _x_
- other sensor value is x
- logical _and_/_or_ for all of the above

Conditions are constructed as a delegate `Func<StateCache, bool>`, where
`StateCache` is an object holding values for all of recent sensor readings.

As stated above, conditions don't have to use the other sensors value, but they
can base their logic on time, daytimes and dates aswell.

### Actions

Actions are the most important part in the automation. They hold the logic that
is supossed to happen when triggered. The representation is a simple `Action`
delegate. Actions can call other services that do some business logic, like:

- device functions downling scheduling
- mail service and mailing
- other notification services...

## Parsing and Compiling

### Overview

To create new block (either Trigger, Condition or action) it is required to
prepare 2 classes:

- **block DTO** that will be parsed and processed, implementing one of:
  - `ITriggerDto`
  - `IConditionDto`
  - `IActionDto`
- **block compiler**, that implements generic `IAutomationBlockCompiler` via one
  of the three block interfaces. The interface exposes 2 methods, that among
  others accept dto argument:
  - `ValidateAsync`
  - `Compile`

> [!IMPORTANT]
>
> The compilers logic will be invoked after successful DTOs parsing. **Invalid
> json is rejected before any of the method runs**

**To properly register block's DTO, the modification of interface DTO is
required** (unfortunately it's a dotnets `System.Text.Json` polymorphic
deserialization limitation).

**The example shows necessary steps**

```cs
// new DTO implementing ITriggerDto, must provide a ServiceKey
// that will be used during runtime compiler resolution
//
// The Discriminator is used to discriminate the derived ITriggerDto type
//
// THEY NEED TO BE THE SAME
public record ExampleTriggerDto(string Name, int Value) : ITriggerDto
{
    public const string Discriminator = "example";
    public string ServiceKey => Discriminator;
}
/* ... */


[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
/* ... */
//
// add new JsonDerivedType attribute
[JsonDerivedType(typeof(ExampleTriggerDto), typeDiscriminator: ExampleTriggerDto.Discriminator)]
public interface ITriggerDto : IAutomationBlock { }
```

> [!IMPORTANT]
>
> **It is very important that _Discriminator_ == _ServiceKey_**

As mentioned earlier the generic `IAutomationBlockCompiler` is not meant to be
implemented directly. There is an interface declared exactly for each block,
like with DTOs:

- `ITriggerCompiler` accepting `ITriggerDto`
- `IConditionCompiler` accepting `IConditionDto`
- `IActionCompiler` accepting `IActionDto`

> [!NOTE]
>
> For the convenience, there is a base generic class for each of the interfaces
> already defined, that allows a person to put its concrete dto (let's say
> `ExampleTriggerDto`) into generics, and the methods override will contain
> concrete types for it's argument, instead of interface.

_example_:

```cs
public class ExampleConditionCompiler : ConditionCompilerBase<ExampleConditionDto>
{
    protected override Func<StateCache, bool> Compile(
        ExampleConditionDto dto, // concrete type instead of IConditionDto
        LookupContext? lookup
    ) { /*...*/ }

    // see Triggers/Example for full example
    protected override async Task<ValidationResult> ValidateAsync(
        ExampleConditionDto dto, // concrete type instead of IConditionDto
        CancellationToken cancellationToken
    ) { /*...*/ }
}
```

### ValidateAsync

Validates the fields of a block's DTO. This includes verifying numeric limits,
resolving database references (devices, sensors, and functions), and validating
block-specific constraints.

**Returns**

A `ValidationResult` representing either:

- **Failure**: Contains a list of BlockError objects.
- **Success**:
  - An empty success (no replacement required).
  - A success containing a `LookupContext` object.

**LookupContext Resolution**

To simplify writing rules, users can reference entities (devices, sensors,
functions) by their friendly names rather than technical _ID_/_DevEUI_. Because
runtime IEventMessage instances only contain raw identifiers, the validator
resolves these names against the database and stores the mapping inside a
`LookupContext`.

During compilation, this context is passed directly to the Compile method,
eliminating redundant database queries.

(_see `./Triggers/Example/ExampleTriggerCompiler.cs`_ for example)

### Compile

Transforms a validated block DTO into an executable automation component. It
uses pre-resolved identifiers from the validation step to avoid additional
database calls.

**Returns**

The compiled delegate specific to the block type:

- **Triggers**: tuple containing
  `(IEvent Event, Func<IEventMessage, bool> Filter)`
- **Conditions**: `Func<StateCache, bool>` predicate delegate
- **Actions**: `Action` delegate

**LookupContext Usage**

If entity resolution was performed during _ValidateAsync_, the populated
`LookupContext` is passed directly into this method. Compilers can retrieve the
resolved technical identifiers using generic method `lookup.Get<T>(key)`

(_see `./Triggers/Example/ExampleTriggerCompiler.cs`_ for example)
