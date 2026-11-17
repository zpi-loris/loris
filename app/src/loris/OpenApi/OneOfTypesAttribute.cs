namespace Loris.OpenApi;

[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class OneOfTypesAttribute(params Type[] types) : Attribute
{
    public Type[] Types => types;
}
