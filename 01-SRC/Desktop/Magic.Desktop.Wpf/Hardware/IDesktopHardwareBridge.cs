using Magic.Contracts.Desktop;

namespace Magic.Desktop.Wpf.Hardware;

/// <summary>
/// Hardware bridge abstraction isolating peripheral drivers (ESC/POS, COM scales, cash drawers)
/// inside the WPF desktop host while keeping shared DTOs in Magic.Contracts.
/// </summary>
public interface IDesktopHardwareBridge
{
    DesktopHostCapabilitiesDto GetCapabilities();
    Task<PrintReceiptResponse> PrintReceiptAsync(PrintReceiptRequest request, CancellationToken cancellationToken = default);
    Task<bool> OpenCashDrawerAsync(string printerName, CancellationToken cancellationToken = default);
    Task<WeighScaleReadingDto> ReadScaleWeightAsync(string portName, int baudRate = 9600, CancellationToken cancellationToken = default);
}

/// <summary>
/// Development stub reporting Windows host bridge readiness with simulated capabilities.
/// Provides safe fallback when physical retail peripherals are not attached.
/// </summary>
public sealed class WindowsHardwareBridgeStub : IDesktopHardwareBridge
{
    public DesktopHostCapabilitiesDto GetCapabilities() => new()
    {
        HostPlatform = "Windows-WPF-WebView2",
        SupportsEscPosRawPrinting = true,
        SupportsCashDrawerPulse = true,
        SupportsSerialWeighScale = false,
        ShellVersion = "1.0.0-remediated"
    };

    public Task<PrintReceiptResponse> PrintReceiptAsync(PrintReceiptRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PrinterName))
        {
            return Task.FromResult(new PrintReceiptResponse
            {
                Success = false,
                ErrorMessage = "Printer name cannot be empty.",
                FallbackToBrowserPrint = true
            });
        }

        // Stub simulates successful spooling
        return Task.FromResult(new PrintReceiptResponse
        {
            Success = true,
            ErrorMessage = null,
            FallbackToBrowserPrint = false
        });
    }

    public Task<bool> OpenCashDrawerAsync(string printerName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    public Task<WeighScaleReadingDto> ReadScaleWeightAsync(string portName, int baudRate = 9600, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new WeighScaleReadingDto
        {
            Success = true,
            WeightKg = 0.000m,
            IsStable = true,
            ErrorMessage = null
        });
    }
}

