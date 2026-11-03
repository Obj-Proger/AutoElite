using AutoElite.Application.Common.Behaviors;
using AutoElite.Application.Common.Messaging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AutoElite.Application;

/// <summary>Registers the application layer in the dependency injection container.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers the dispatcher, all request handlers and validators found in this assembly,
    /// and the pipeline behaviors in execution order: logging, then validation.
    /// Also registers the system clock as <see cref="TimeProvider"/> unless a clock is already registered,
    /// so that tests can substitute their own.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IDispatcher, Dispatcher>();

        RegisterOpenGenericImplementations(services, typeof(IRequestHandler<,>));
        RegisterOpenGenericImplementations(services, typeof(IValidator<>));

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }

    private static void RegisterOpenGenericImplementations(IServiceCollection services, Type openGenericInterface)
    {
        var applicationAssembly = typeof(DependencyInjection).Assembly;

        foreach (var implementationType in applicationAssembly.GetTypes())
        {
            if (implementationType.IsAbstract || implementationType.IsInterface)
                continue;

            foreach (var implementedInterface in implementationType.GetInterfaces())
            {
                if (implementedInterface.IsGenericType && implementedInterface.GetGenericTypeDefinition() == openGenericInterface)
                    services.AddScoped(implementedInterface, implementationType);
            }
        }
    }
}
