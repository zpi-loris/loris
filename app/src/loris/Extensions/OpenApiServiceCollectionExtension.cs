using Loris.Models;

namespace Loris.Extensions;

public static class OpenApiServiceCollectionExtension
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddLorisOpenApi()
        {
            services.AddOpenApi(options =>
            {
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
                options.AddSchemaTransformer(
                    (schema, context, _) =>
                    {
                        // Custom schema for DevEui type
                        // NOTE: KEEP IN SYNC WITH DevEuiJsonConverter
                        if (context.JsonTypeInfo.Type == typeof(DevEui))
                        {
                            schema.Type = Microsoft.OpenApi.JsonSchemaType.String;
                            schema.MaxLength = 16;
                            schema.MinLength = 16;
                            schema.Pattern = "^[0-9a-fA-F]{16}$";
                        }

                        return Task.CompletedTask;
                    }
                );
            });
            return services;
        }
    }
}
