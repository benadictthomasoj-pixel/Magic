using Asp.Versioning;
using Magic.Api.Middleware;
using Magic.Application;
using Magic.Contracts.Headers;
using Magic.Infrastructure;
using Magic.Platform;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddMagicPlatform();
builder.Services.AddMagicApplication();
builder.Services.AddMagicInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();
builder.Services.AddAuthorization();

builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddMvc()
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'VVV";
        options.SubstituteApiVersionInUrl = true;
    })
    .AddOpenApi();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("MagicClientPolicy", policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .WithExposedHeaders("X-Correlation-Id", "api-supported-versions");
        }
    });
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<RequestContextMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        if (httpContext.Items.TryGetValue(MagicHttpHeaders.CorrelationId, out var cid) && cid is not null)
        {
            diagnosticContext.Set("CorrelationId", cid);
        }

        if (httpContext.Items.TryGetValue(MagicHttpHeaders.BranchId, out var bid) && bid is not null)
        {
            diagnosticContext.Set("BranchId", bid);
        }

        if (httpContext.Items.TryGetValue(MagicHttpHeaders.TerminalId, out var tid) && tid is not null)
        {
            diagnosticContext.Set("TerminalId", tid);
        }
    };
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().WithDocumentPerVersion();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors("MagicClientPolicy");
app.UseAuthorization();
app.UseMiddleware<IdempotencyMiddleware>();

app.MapControllers();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = registration => registration.Tags.Contains("ready")
});

var autoSeed = builder.Configuration.GetValue<bool>("Seeding:AutoSeedDevelopmentData", defaultValue: true);
if (app.Environment.IsDevelopment() && autoSeed)
{
    await Magic.Infrastructure.Persistence.AccountingDatabaseSeeder.SeedDevelopmentAccountingAsync(app.Services);
}

app.Run();

/// <summary>
/// Public entry point type marker supporting WebApplicationFactory integration testing.
/// </summary>
public partial class Program;

