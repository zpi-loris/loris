using System.Diagnostics.CodeAnalysis;

namespace Loris.Automations;

public sealed record LookupContext(IReadOnlyDictionary<string, object> Values)
{
    public T Get<T>(string key)
    {
        if (!Values.TryGetValue(key, out var val))
            throw new KeyNotFoundException($"Key '{key}' was not resolved during validation.");

        return (T)val;
    }

    public bool TryGet<T>(string key, [MaybeNullWhen(false)] out T value)
    {
        var ret = Values.TryGetValue(key, out var objVal);
        if (objVal is not null)
            value = (T)objVal;
        else
            value = default;
        return ret;
    }

    public static LookupContext FromDict(Dictionary<string, object> valuePairs) =>
        new(valuePairs.AsReadOnly());
}
