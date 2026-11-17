using System.Text;
using Asp.Versioning;
using Magic.Api.Middleware;
using Magic.Application;
using Magic.Contracts.Headers;
using Magic.Infrastructure;
using Magic.Infrastructure.Security;
using Magic.Platform;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.AddMagicPlatform();
builder.Services.AddMagicApplication();
builder.Services.AddMagicInfrastructure(builder.Configuration);

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddControllers();

// JWT Bearer Authentication & Authorization (Phase A)
var jwtConfig = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig.SecretKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtConfig.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtConfig.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

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
app.UseAuthentication();
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
    using var scope = app.Services.CreateScope();
    var securitySeeder = scope.ServiceProvider.GetRequiredService<Magic.Infrastructure.Persistence.SecurityDatabaseSeeder>();
    await securitySeeder.SeedAsync();

    await Magic.Infrastructure.Persistence.AccountingDatabaseSeeder.SeedDevelopmentAccountingAsync(app.Services);

    var commercialSeeder = scope.ServiceProvider.GetRequiredService<Magic.Infrastructure.Persistence.CommercialDatabaseSeeder>();
    await commercialSeeder.SeedAsync();
}

app.Run();

/// <summary>
/// Public entry point type marker supporting WebApplicationFactory integration testing.
/// </summary>
public partial class Program;

