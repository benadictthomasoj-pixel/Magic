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

    public static DesktopRuntimeSettings CreateDefault() => new()
    {
        DeploymentMode = "SingleComputer",
        ApiBaseUri = new Uri("http://127.0.0.1:5000", UriKind.Absolute),
        FrontendEntryUri = new Uri("http://127.0.0.1:4200", UriKind.Absolute)
    };
}

