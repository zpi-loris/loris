namespace Loris.Json;

/// <summary>
/// Allows for json serialization/deserialization to opt in for specified types.
/// </summary>
/// <remarks>
/// Remember to put <b>string</b> last, because it is most permisive
/// </remarks>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class JsonOneOfTypesAttribute(params Type[] types) : Attribute
{
    public Type[] Types => types;
}
