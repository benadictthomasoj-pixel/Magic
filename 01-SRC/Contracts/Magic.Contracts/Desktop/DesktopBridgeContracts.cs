namespace Magic.Contracts.Desktop;

/// <summary>
/// Envelope exchanged across the WPF <-> WebView2 postMessage hardware bridge.
/// Keeps shared desktop/frontend contract types centralized in Magic.Contracts.
/// </summary>
public sealed record DesktopBridgeMessage
{
    public required string MessageType { get; init; }
    public required string CorrelationId { get; init; }
    public string? PayloadJson { get; init; }
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Describes host capabilities surfaced from WPF desktop shell to Angular UI.
/// </summary>
public sealed record DesktopHostCapabilitiesDto
{
    public required string HostPlatform { get; init; }
    public required bool SupportsEscPosRawPrinting { get; init; }
    public required bool SupportsCashDrawerPulse { get; init; }
    public required bool SupportsSerialWeighScale { get; init; }
    public required string ShellVersion { get; init; }
}

