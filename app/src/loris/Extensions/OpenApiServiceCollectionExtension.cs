using System.Text.Json;
using System.Text.Json.Serialization;
using Loris.Json;
using Loris.Models;
using Loris.OpenApi;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.OpenApi;

namespace Loris.Extensions;

public static class OpenApiServiceCollectionExtension
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddLorisOpenApi()
        {
            services.AddOpenApi(options =>
            {
                // add document info
                options.AddDocumentTransformer(
                    (document, _, _) =>
                    {
                        document.Info.Title = "Loris API";
                        document.Info.Description =
                            "Loris API for device management and automation";
                        document.Info.Version = "1.0.0";
                        return Task.CompletedTask;
                    }
                );

                // add schema transformer
                options.AddSchemaTransformer(
                    async (schema, context, ct) =>
                    {
                        // Custom schema for DevEui type
                        // NOTE: KEEP IN SYNC WITH DevEuiJsonConverter
                        //
                        if (context.JsonTypeInfo.Type == typeof(DevEui))
                        {
                            schema.Type = JsonSchemaType.String;
                            schema.MaxLength = 16;
                            schema.MinLength = 16;
                            schema.Pattern = "^[0-9a-fA-F]{16}$";
                        }

                        // Specificaly created for CallFunctionRequest
                        // and it's oneof arg type
                        var attr = context
                            .JsonPropertyInfo?.AttributeProvider?.GetCustomAttributes(
                                typeof(JsonOneOfTypesAttribute),
                                false
                            )
                            .OfType<JsonOneOfTypesAttribute>()
                            .FirstOrDefault();

                        if (attr is not null)
                        {
                            schema.Type = null;
                            schema.OneOf = [];

                            foreach (var type in attr.Types)
                            {
                                var subSchema = await context.GetOrCreateSchemaAsync(
                                    type,
                                    null,
                                    ct
                                );
                                schema.OneOf.Add(subSchema);
                            }
                        }

                        return;
                    }
                );
            });

            // This is for open api reference only. Unfortunately,
            // json serialization options defined for controllers have no respect
            // in open api, according to docs:
            // https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/include-metadata?view=aspnetcore-10.0&tabs=minimal-apis#mvc-json-options-and-global-json-options
            services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.Converters.Add(
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
                );
            });

            return services;
        }
    }
}
