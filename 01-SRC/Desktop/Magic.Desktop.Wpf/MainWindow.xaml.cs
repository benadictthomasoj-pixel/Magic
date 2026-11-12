using System.Text.Json;
using System.Windows;
using Magic.Contracts.Desktop;
using Magic.Desktop.Wpf.Configuration;
using Magic.Desktop.Wpf.Hardware;
using Microsoft.Web.WebView2.Core;

namespace Magic.Desktop.Wpf;

/// <summary>
/// Main WPF shell hosting WebView2 and mediating native hardware bridge messages cleanly.
/// </summary>
public partial class MainWindow : Window
{
    private static readonly JsonSerializerOptions BridgeJsonOptions = new(JsonSerializerDefaults.Web);
    private readonly DesktopRuntimeSettings _settings = DesktopRuntimeSettings.CreateDefault();
    private readonly IDesktopHardwareBridge _hardwareBridge;

    public MainWindow() : this(new WindowsHardwareBridgeStub())
    {
    }

    public MainWindow(IDesktopHardwareBridge hardwareBridge)
    {
        ArgumentNullException.ThrowIfNull(hardwareBridge);
        _hardwareBridge = hardwareBridge;
        InitializeComponent();
        Loaded += OnWindowLoadedAsync;
    }

    private async void OnWindowLoadedAsync(object sender, RoutedEventArgs e)
    {
        try
        {
            await MagicWebView.EnsureCoreWebView2Async();

            MagicWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            MagicWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
            MagicWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

            MagicWebView.Source = _settings.FrontendEntryUri;
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unable to initialize WebView2 runtime: {ex.Message}",
                "MAGIC Desktop Startup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var envelope = JsonSerializer.Deserialize<DesktopBridgeMessage>(e.WebMessageAsJson, BridgeJsonOptions);
        if (envelope is null)
        {
            return;
        }

        if (string.Equals(envelope.MessageType, "desktop.capabilities.query", StringComparison.Ordinal))
        {
            var capabilities = _hardwareBridge.GetCapabilities();
            var response = new DesktopBridgeMessage
            {
                MessageType = "desktop.capabilities.response",
                CorrelationId = envelope.CorrelationId,
                PayloadJson = JsonSerializer.Serialize(capabilities, BridgeJsonOptions),
                TimestampUtc = DateTimeOffset.UtcNow
            };

            MagicWebView.CoreWebView2.PostWebMessageAsJson(JsonSerializer.Serialize(response, BridgeJsonOptions));
        }
    }
}

