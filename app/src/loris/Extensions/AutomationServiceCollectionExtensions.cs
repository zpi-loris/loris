using System.Reflection;
using Loris.Automations.Actions;
using Loris.Automations.Conditions;
using Loris.Automations.Triggers;

namespace Loris.Extensions;

public static class AutomationServiceCollectionExtensions
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAutomationsCompilers()
        {
            services.Scan(scan =>
                scan.FromAssemblyOf<ITriggerCompiler>()
                    .AddClasses(classes => classes.AssignableTo<ITriggerCompiler>())
                    .AsImplementedInterfaces()
                    .WithServiceKey(GetCompilerServiceKey)
                    .WithSingletonLifetime()
            );

            services.Scan(scan =>
                scan.FromAssemblyOf<IConditionCompiler>()
                    .AddClasses(classes => classes.AssignableTo<IConditionCompiler>())
                    .AsImplementedInterfaces()
                    .WithServiceKey(GetCompilerServiceKey)
                    .WithSingletonLifetime()
            );

            services.Scan(scan =>
                scan.FromAssemblyOf<IActionCompiler>()
                    .AddClasses(classes => classes.AssignableTo<IActionCompiler>())
                    .AsImplementedInterfaces()
                    .WithServiceKey(GetCompilerServiceKey)
                    .WithSingletonLifetime()
            );

            return services;
        }
    }

    // NOTE: HELPER BELOW IS FULLY AI GENERATED
    // if any error occurs, it's better to manually register compilers
    // in Program.cs like so:
    //
    // builder.Services.AddKeyedSingleton<ITriggerCompiler, TestValidTriggerCompiler>(
    //     TestValidTriggerDto.Discriminator
    // );
    public static object GetCompilerServiceKey(Type compilerType)
    {
        // Walk base types to find the generic compiler base class
        Type? current = compilerType.BaseType;
        Type? dtoType = null;

        while (current != null && current != typeof(object))
        {
            if (current.IsGenericType)
            {
                // Matches TriggerCompilerBase<TDto>, ActionCompilerBase<TDto>, etc.
                dtoType = current.GetGenericArguments()[0];
                break;
            }
            current = current.BaseType;
        }

        if (dtoType == null)
        {
            throw new InvalidOperationException(
                $"Compiler '{compilerType.FullName}' must inherit from a generic base class specifying its DTO (e.g., TriggerCompilerBase<TDto>)."
            );
        }

        // Read constant/static field 'Discriminator' (e.g. public const string Discriminator = "example")
        var field = dtoType.GetField("Discriminator", BindingFlags.Public | BindingFlags.Static);
        if (field != null)
        {
            return field.GetValue(null)
                ?? throw new InvalidOperationException(
                    $"'Discriminator' on '{dtoType.Name}' returned null."
                );
        }

        throw new InvalidOperationException(
            $"DTO '{dtoType.FullName}' must define a public static or const 'Discriminator' field/property."
        );
    }
}
