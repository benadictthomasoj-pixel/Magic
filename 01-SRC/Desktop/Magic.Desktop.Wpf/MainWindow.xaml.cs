using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using Magic.Contracts.Desktop;
using Magic.Desktop.Wpf.Configuration;
using Magic.Desktop.Wpf.Hardware;
using Microsoft.Web.WebView2.Core;

namespace Magic.Desktop.Wpf;

/// <summary>
/// Main WPF shell hosting WebView2 and mediating native hardware bridge messages cleanly.
/// Enforces UserDataFolder isolation, navigation allowlists, and origin validation on all IPC messages.
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
        Closing += OnWindowClosing;
    }

    private async void OnWindowLoadedAsync(object sender, RoutedEventArgs e)
    {
        try
        {
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MAGIC",
                "WebView2");

            Directory.CreateDirectory(userDataFolder);

            var environment = await CoreWebView2Environment.CreateAsync(
                browserExecutableFolder: null,
                userDataFolder: userDataFolder);

            await MagicWebView.EnsureCoreWebView2Async(environment);

            MagicWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
            MagicWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;

#if !DEBUG
            MagicWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
            MagicWebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
#endif

            MagicWebView.CoreWebView2.NavigationStarting += OnNavigationStarting;
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

    private void OnNavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var targetUri) || !IsAllowedOrigin(targetUri))
        {
            e.Cancel = true;
        }
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        // Enforce origin validation: reject messages from arbitrary or unapproved external web contexts
        if (!Uri.TryCreate(e.Source, UriKind.Absolute, out var sourceUri) || !IsAllowedOrigin(sourceUri))
        {
            return;
        }

        DesktopBridgeMessage? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<DesktopBridgeMessage>(e.WebMessageAsJson, BridgeJsonOptions);
        }
        catch
        {
            // Discard malformed JSON without disrupting the UI dispatcher thread
            return;
        }

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

            MagicWebView.CoreWebView2?.PostWebMessageAsJson(JsonSerializer.Serialize(response, BridgeJsonOptions));
        }
    }

    private bool IsAllowedOrigin(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        if (Uri.Compare(uri, _settings.FrontendEntryUri, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) == 0)
        {
            return true;
        }

        if (string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return uri.Host.EndsWith(".magic.local", StringComparison.OrdinalIgnoreCase);
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (MagicWebView.CoreWebView2 is not null)
        {
            MagicWebView.CoreWebView2.NavigationStarting -= OnNavigationStarting;
            MagicWebView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
        }

        MagicWebView.Dispose();
    }
}

