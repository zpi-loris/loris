namespace Loris.Automations;

public sealed record LookupContext(IReadOnlyDictionary<string, object> Values)
{
    public T Get<T>(string key)
    {
        if (!Values.TryGetValue(key, out var val))
            throw new KeyNotFoundException($"Key '{key}' was not resolved during validation.");

        return (T)val;
    }

    public static LookupContext FromDict(Dictionary<string, object> valuePairs) =>
        new(valuePairs.AsReadOnly());
}
