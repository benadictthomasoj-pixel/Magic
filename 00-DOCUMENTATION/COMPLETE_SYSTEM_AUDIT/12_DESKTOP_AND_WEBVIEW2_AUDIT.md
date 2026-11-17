# MAGIC ERP — SYSTEM-WIDE ARCHITECTURE, CODEBASE & INTEGRATION AUDIT
## Document 12: Desktop Shell, WebView2 Hosting & Hardware Interop Audit

**Document ID:** `AUDIT-12-DESKTOP`  
**Audit Scope:** WPF Host Architecture, Microsoft WebView2 Chromium Engine, IPC Messaging, Hardware Integration  
**Workspace:** `/Users/benadict/Documents/antigravity projects/magic`  
**Git Baseline Commit:** `2567c7d`  

---

### 1. Desktop Shell Architecture Overview

The desktop runtime is located under `01-SRC/Desktop/Magic.Desktop.Wpf/`. It provides a dedicated native Windows container for cashier workstations, branch terminals, and back-office users who require tight operating system integration (e.g. receipt printers, barcode scanners, and cash drawers).

```
┌─────────────────────────────────────────────────────────────┐
│             Windows 10 / 11 Host Operating System           │
├─────────────────────────────────────────────────────────────┤
│             WPF Application Container (.NET 10)             │
│   MainWindow.xaml.cs, App.xaml.cs, DesktopRuntimeSettings   │
│                                                             │
│   ┌─────────────────────────────────────────────────────┐   │
│   │       Microsoft WebView2 Runtime (Chromium)         │   │
│   │   - Origin Whitelisting Interceptor                 │   │
│   │   - Isolated Profile: %LOCALAPPDATA%\MagicERP       │   │
│   │   - JavaScript <---> C# Bidirectional Bridge        │   │
│   │                                                     │   │
│   │   ┌─────────────────────────────────────────────┐   │   │
│   │   │         Angular 22 SPA Application          │   │   │
│   │   │   POS Workspace, Reports, Workspaces        │   │   │
│   │   └─────────────────────────────────────────────┘   │   │
│   └──────────────────────────┬──────────────────────────┘   │
│                              │ IPC (chrome.webview.postMessage)
│                              ▼                              │
│   ┌─────────────────────────────────────────────────────┐   │
│   │          IDesktopHardwareBridge (Abstract)          │   │
│   │   - Printer: ESC/POS Thermal Spooler                │   │
│   │   - Drawer: Solenoid Kick Pulse                     │   │
│   │   - Scale: RS-232 Serial Weighing                   │   │
│   └──────────────────────────┬──────────────────────────┘   │
│                              │                              │
│                              ▼                              │
│   ┌─────────────────────────────────────────────────────┐   │
│   │         WindowsHardwareBridgeStub (CURRENT)         │   │
│   │   (Simulated in-memory response to queries)         │   │
│   └─────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
```

---

### 2. Runtime Security & Sandboxing Audit

1. **Origin Restriction & Anti-Phishing Guard:**
   In `MainWindow.xaml.cs`, `CoreWebView2_NavigationStarting` intercepts all URL navigations before network requests are dispatched:
   ```csharp
   private void CoreWebView2_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
   {
       if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri))
       {
           e.Cancel = true;
           return;
       }
       bool isAllowed = (uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host.EndsWith(".magic.local"))
                        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
       if (!isAllowed)
       {
           e.Cancel = true; // Block external redirection
       }
   }
   ```
   **Audit Result:** Prevents arbitrary web browsing, external script injection, or phishing attacks within the POS desktop shell.

2. **Profile & Cookie Isolation:**
   The WebView2 environment is explicitly initialized with an isolated user data directory:
   `Path.Combine(Environment.GetFolderPath(SpecialFolder.LocalApplicationData), "MagicERP", "WebView2Profile")`.
   This ensures session tokens and local storage cannot be accessed by external web browsers installed on the workstation.

---

### 3. Hardware Bridge & Physical Device Status

#### Interface Specification (`IDesktopHardwareBridge.cs`)
The bridge defines standard enterprise POS hardware operations:
- `desktop.capabilities.query` — Returns supported hardware peripherals.
- `desktop.printer.printReceipt` — Prints raw ESC/POS byte buffers to thermal receipt printers.
- `desktop.drawer.open` — Sends solenoid kick pulse to open cash drawer.
- `desktop.scale.readWeight` — Reads current gross/tare weight from serial RS-232 scale.

#### Audit Finding on Bridge Implementation:
Currently, the registered bridge is `WindowsHardwareBridgeStub.cs`:
- Responds to `desktop.capabilities.query` returning simulated printer, drawer, and scanner flags.
- Does NOT interact with the Windows Win32 print spooler (`winspool.drv`) for ESC/POS raw printing.
- Does NOT open COM ports (`System.IO.Ports.SerialPort`) for electronic weighing scales.
- When `printReceipt` or `openDrawer` is invoked from JavaScript, the stub logs the command to debug output without actuating physical hardware.

#### Recommendation for Physical Retail Deployment:
Implement `WindowsNativeHardwareBridge.cs` utilizing:
1. `System.Drawing.Printing.PrinterSettings` and P/Invoke `RawPrinterHelper` for native ESC/POS thermal printing.
2. `System.IO.Ports` for continuous scale weight reading at POS checkout counters.

