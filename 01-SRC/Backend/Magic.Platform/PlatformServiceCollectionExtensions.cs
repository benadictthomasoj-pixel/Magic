using Magic.Application.Abstractions;
using Magic.Platform.Options;
using Magic.Platform.Runtime;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

namespace Magic.Platform;

/// <summary>
/// Configures MAGIC Platform services: Serilog, OpenTelemetry, Windows Service readiness, and options validation.
/// </summary>
public static class PlatformServiceCollectionExtensions
{
    public const string ActivitySourceName = "Magic.Erp";

    public static WebApplicationBuilder AddMagicPlatform(this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Windows Service lifecycle integration (active when installed via sc/New-Service on Windows hosts)
        builder.Host.UseWindowsService(options =>
        {
            options.ServiceName = builder.Configuration["Deployment:ServiceName"] ?? "MagicBackend";
        });

        builder.Services.AddOptions<MagicDeploymentOptions>()
            .BindConfiguration(MagicDeploymentOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddOptions<MagicSecurityOptions>()
            .BindConfiguration(MagicSecurityOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        builder.Services.AddSingleton<IRuntimeEnvironmentInfo, RuntimeEnvironmentInfo>();

        // Structured logging via Serilog (reads minimum levels/sinks from appsettings + enriches log context)
        builder.Services.AddSerilog((services, loggerConfig) => loggerConfig
            .ReadFrom.Configuration(builder.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "MAGIC.Backend"));

        // Local-first observability via OpenTelemetry (no external cloud collectors)
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("Magic.Api"))
            .WithTracing(tracing => tracing
                .AddSource(ActivitySourceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());

        return builder;
    }
}

