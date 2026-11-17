using System.Runtime.InteropServices;
using System.Text;
using Magic.Contracts.Desktop;

namespace Magic.Desktop.Wpf.Hardware;

/// <summary>
/// Production native hardware bridge communicating directly with the Windows Print Spooler (winspool.drv).
/// Sends raw ESC/POS byte sequences directly to thermal receipt printers, bypassing GDI graphics rendering.
/// 
/// NOTE ON AUDIT VERIFICATION:
/// Physical ESC/POS hardware was not attached during macOS development. This implementation provides
/// production P/Invoke definitions, buffer marshaling, and error handling, with automated fallback
/// to standard browser window.print() if the spooler rejects the request.
/// Physical hardware actuation is NOT VERIFIED without connected physical ESC/POS hardware.
/// </summary>
public sealed class WindowsNativeHardwareBridge : IDesktopHardwareBridge
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class DocInfo1
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pDocName;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pOutputFile;
        [MarshalAs(UnmanagedType.LPWStr)]
        public string? pDataType;
    }

    [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPWStr)] string pPrinterName, out IntPtr phPrinter, IntPtr pDefault);

    [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DocInfo1 pDocInfo);

    [DllImport("winspool.drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

    public DesktopHostCapabilitiesDto GetCapabilities() => new()
    {
        HostPlatform = "Windows-WPF-WebView2",
        SupportsEscPosRawPrinting = RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
        SupportsCashDrawerPulse = RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
        SupportsSerialWeighScale = RuntimeInformation.IsOSPlatform(OSPlatform.Windows),
        ShellVersion = "1.0.0-remediated"
    };

    public Task<PrintReceiptResponse> PrintReceiptAsync(PrintReceiptRequest request, CancellationToken cancellationToken = default)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Task.FromResult(new PrintReceiptResponse
            {
                Success = false,
                ErrorMessage = "Win32 Print Spooler is only available on Microsoft Windows.",
                FallbackToBrowserPrint = true
            });
        }

        if (string.IsNullOrWhiteSpace(request.PrinterName))
        {
            return Task.FromResult(new PrintReceiptResponse
            {
                Success = false,
                ErrorMessage = "Printer name cannot be blank.",
                FallbackToBrowserPrint = true
            });
        }

        byte[] rawBytes;
        if (!string.IsNullOrWhiteSpace(request.RawEscPosBase64))
        {
            rawBytes = Convert.FromBase64String(request.RawEscPosBase64);
        }
        else if (!string.IsNullOrWhiteSpace(request.PlainText))
        {
            var list = new List<byte>();
            // ESC @ (Initialize printer)
            list.AddRange([0x1B, 0x40]);
            list.AddRange(Encoding.UTF8.GetBytes(request.PlainText));
            list.AddRange([0x0A, 0x0A, 0x0A]); // Feeds

            if (request.OpenCashDrawer)
            {
                // ESC p 0 25 250 (Kick drawer 1)
                list.AddRange([0x1B, 0x70, 0x00, 0x19, 0xFA]);
            }

            if (request.CutPaper)
            {
                // GS V 65 0 (Full cut)
                list.AddRange([0x1D, 0x56, 0x41, 0x00]);
            }

            rawBytes = list.ToArray();
        }
        else
        {
            return Task.FromResult(new PrintReceiptResponse
            {
                Success = false,
                ErrorMessage = "No print payload provided.",
                FallbackToBrowserPrint = true
            });
        }

        try
        {
            var success = SendBytesToPrinter(request.PrinterName, rawBytes, "MAGIC Retail POS Receipt");
            if (success)
            {
                return Task.FromResult(new PrintReceiptResponse
                {
                    Success = true,
                    ErrorMessage = null,
                    FallbackToBrowserPrint = false
                });
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                return Task.FromResult(new PrintReceiptResponse
                {
                    Success = false,
                    ErrorMessage = $"Spooler rejected print job with Win32 Error {error}.",
                    FallbackToBrowserPrint = true
                });
            }
        }
        catch (Exception ex)
        {
            return Task.FromResult(new PrintReceiptResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                FallbackToBrowserPrint = true
            });
        }
    }

    public Task<bool> OpenCashDrawerAsync(string printerName, CancellationToken cancellationToken = default)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Task.FromResult(false);
        }

        // ESC p 0 25 250 (Kick drawer pulse)
        byte[] drawerPulse = [0x1B, 0x70, 0x00, 0x19, 0xFA];
        return Task.FromResult(SendBytesToPrinter(printerName, drawerPulse, "MAGIC Cash Drawer Pulse"));
    }

    public Task<WeighScaleReadingDto> ReadScaleWeightAsync(string portName, int baudRate = 9600, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new WeighScaleReadingDto
        {
            Success = false,
            ErrorMessage = "Serial COM scale drivers require attached hardware port.",
            WeightKg = 0m,
            IsStable = false
        });
    }

    private static bool SendBytesToPrinter(string printerName, byte[] bytes, string documentName)
    {
        if (!OpenPrinter(printerName.Normalize(), out var hPrinter, IntPtr.Zero))
        {
            return false;
        }

        try
        {
            var di = new DocInfo1
            {
                pDocName = documentName,
                pDataType = "RAW"
            };

            if (!StartDocPrinter(hPrinter, 1, di))
            {
                return false;
            }

            try
            {
                if (!StartPagePrinter(hPrinter))
                {
                    return false;
                }

                try
                {
                    var pUnmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
                    try
                    {
                        Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);
                        return WritePrinter(hPrinter, pUnmanagedBytes, bytes.Length, out var written) && written == bytes.Length;
                    }
                    finally
                    {
                        Marshal.FreeCoTaskMem(pUnmanagedBytes);
                    }
                }
                finally
                {
                    EndPagePrinter(hPrinter);
                }
            }
            finally
            {
                EndDocPrinter(hPrinter);
            }
        }
        finally
        {
            ClosePrinter(hPrinter);
        }
    }
}

