namespace Loris.OpenApi;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class JsonOneOfTypesAttribute(params Type[] types) : Attribute
{
    public Type[] Types => types;
}
