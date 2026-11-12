using Magic.Contracts.Desktop;

namespace Magic.Desktop.Wpf.Hardware;

/// <summary>
/// Hardware bridge abstraction isolating peripheral drivers (ESC/POS, COM scales, cash drawers)
/// inside the WPF desktop host while keeping shared DTOs in Magic.Contracts.
/// </summary>
public interface IDesktopHardwareBridge
{
    DesktopHostCapabilitiesDto GetCapabilities();
}

/// <summary>
/// Phase 1 foundation stub reporting Windows host bridge readiness without premature hardware dependencies.
/// </summary>
public sealed class WindowsHardwareBridgeStub : IDesktopHardwareBridge
{
    public DesktopHostCapabilitiesDto GetCapabilities() => new()
    {
        HostPlatform = "Windows-WPF-WebView2",
        SupportsEscPosRawPrinting = false,
        SupportsCashDrawerPulse = false,
        SupportsSerialWeighScale = false,
        ShellVersion = "0.1.0-phase1"
    };
}

