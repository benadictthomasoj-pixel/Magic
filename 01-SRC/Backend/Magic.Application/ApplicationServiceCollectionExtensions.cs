using FluentValidation;
using Magic.Application.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Magic.Application;

/// <summary>
/// Registers MAGIC Application layer orchestrators and validators.
/// </summary>
public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddMagicApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceCollectionExtensions).Assembly);
        services.AddScoped<ISystemDiagnosticsService, SystemDiagnosticsService>();
        return services;
    }
}

