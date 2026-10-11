namespace CodeHub.Application;

using System.Reflection;
using CodeHub.Application.Common;
using Microsoft.Extensions.DependencyInjection;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddSingleton<IDispatcher, Dispatcher>();

        RegisterHandlers(services, assembly);
        RegisterValidators(services, assembly);

        return services;
    }

    private static void RegisterHandlers(IServiceCollection services, Assembly assembly)
    {
        var openHandlerTypes = new[]
        {
            typeof(ICommandHandler<>),
            typeof(ICommandHandler<,>),
            typeof(IQueryHandler<,>)
        };

        foreach (var type in assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            foreach (var handlerInterface in type
                         .GetInterfaces()
                         .Where(i => i.IsGenericType && openHandlerTypes.Contains(i.GetGenericTypeDefinition())))
            {
                services.AddTransient(handlerInterface, type);
            }
        }
    }

    private static void RegisterValidators(IServiceCollection services, Assembly assembly)
    {
        foreach (var type in assembly.GetTypes().Where(t => t is { IsClass: true, IsAbstract: false }))
        {
            foreach (var validatorInterface in type
                         .GetInterfaces()
                         .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>)))
            {
                services.AddSingleton(validatorInterface, type);
            }
        }
    }

}
