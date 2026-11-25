using System.Text.Json.Serialization.Metadata;
using Loris.OpenApi;

namespace Loris.Json;

public static class OneOfTypesPropertyModifier
{
    public static void Modifier(JsonTypeInfo jsonTypeInfo)
    {
        if (jsonTypeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        foreach (var property in jsonTypeInfo.Properties)
        {
            var attr =
                property
                    .AttributeProvider?.GetCustomAttributes(
                        typeof(JsonOneOfTypesAttribute),
                        inherit: true
                    )
                    .FirstOrDefault() as JsonOneOfTypesAttribute;

            if (attr is not null)
            {
                property.CustomConverter = new OneOfTypeConverter(attr.Types);
            }
        }
    }
}
