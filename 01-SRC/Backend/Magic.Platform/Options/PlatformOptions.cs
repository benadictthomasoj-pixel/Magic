using System.ComponentModel.DataAnnotations;

namespace Magic.Platform.Options;

/// <summary>
/// Deployment topology and local-first operational settings.
/// </summary>
public sealed class MagicDeploymentOptions
{
    public const string SectionName = "Deployment";

    [Required]
    public string Mode { get; init; } = "SingleComputer";

    [Required]
    public string ServiceName { get; init; } = "MagicBackend";

    [Required]
    public string NetworkPolicy { get; init; } = "StrictOnlineLan";
}

/// <summary>
/// Security boundary configuration. Never stores secrets or cryptographic signing keys in code or JSON templates.
/// </summary>
public sealed class MagicSecurityOptions
{
    public const string SectionName = "Security";

    [Required]
    public string Authority { get; init; } = "DEFERRED_OPEN_003";

    public bool RequireApprovedTerminalHeader { get; init; }
}

