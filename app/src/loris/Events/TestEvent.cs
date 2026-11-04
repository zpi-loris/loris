namespace Loris.Events;

public record TestEvent : IEvent<TestEvent>
{
    public required string Name { get; init; }

    public static TestEvent Default => new() { Name = "Test" };
}
