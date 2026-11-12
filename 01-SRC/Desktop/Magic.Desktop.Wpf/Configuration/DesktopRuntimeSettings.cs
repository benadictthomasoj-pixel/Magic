namespace Magic.Desktop.Wpf.Configuration;

/// <summary>
/// Local workstation settings distinguishing SingleComputer loopback vs. LocalNetwork API host.
/// Desktop clients never hold database connection strings or access PostgreSQL directly.
/// </summary>
public sealed record DesktopRuntimeSettings
{
    public required string DeploymentMode { get; init; }
    public required Uri ApiBaseUri { get; init; }
    public required Uri FrontendEntryUri { get; init; }

    public static DesktopRuntimeSettings CreateDefault(string[]? args = null)
    {
        var apiEnv = Environment.GetEnvironmentVariable("MAGIC_API_BASE_URL");
        var frontendEnv = Environment.GetEnvironmentVariable("MAGIC_FRONTEND_URL");

        string? apiArg = null;
        string? frontendArg = null;

        if (args is not null)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "--api-url", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    apiArg = args[i + 1];
                }
                else if (string.Equals(args[i], "--frontend-url", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    frontendArg = args[i + 1];
                }
            }
        }

        var apiCandidate = apiArg ?? apiEnv ?? "http://127.0.0.1:5000";
        var frontendCandidate = frontendArg ?? frontendEnv ?? "http://127.0.0.1:4200";

        var apiUri = Uri.TryCreate(apiCandidate, UriKind.Absolute, out var parsedApi)
            ? parsedApi
            : new Uri("http://127.0.0.1:5000", UriKind.Absolute);

        var frontendUri = Uri.TryCreate(frontendCandidate, UriKind.Absolute, out var parsedFrontend)
            ? parsedFrontend
            : new Uri("http://127.0.0.1:4200", UriKind.Absolute);

        var isLocal = string.Equals(apiUri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
                      string.Equals(apiUri.Host, "localhost", StringComparison.OrdinalIgnoreCase);

        return new DesktopRuntimeSettings
        {
            DeploymentMode = isLocal ? "SingleComputer" : "LocalNetwork",
            ApiBaseUri = apiUri,
            FrontendEntryUri = frontendUri
        };
    }
}

