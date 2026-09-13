using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TimeTracker.Application.Common.Behaviours;

namespace TimeTracker.Application;

/// <summary>
/// Composition-root extension for wiring up the Application layer. Keeping this here
/// (rather than in the API project) means the API only needs to know "Add the Application
/// layer", respecting the Dependency Inversion Principle.
/// </summary>
public static class DependencyInjection {
    public static IServiceCollection AddApplication(this IServiceCollection services) {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

        return services;
    }
}