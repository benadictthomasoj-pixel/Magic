# MAGIC ERP — PRODUCTION READINESS AUDIT & RELEASE GATE
## Document 09: Windows Desktop Release Readiness & Security Verification

**Document ID:** `PR-09-WINDOWS-RELEASE`  
**Host Platform:** macOS Apple Silicon ARM64 (Build Verification Host)  
**Target Deployment OS:** Windows 10 (21H2+) / Windows 11 (64-bit x64)  
**Desktop Shell:** WPF (.NET 10.0-windows) + Microsoft Edge WebView2  
**Printing Architecture:** Win32 `winspool.drv` P/Invoke + ESC/POS Raw Byte Spooling  
**Date:** November 14, 2026  

---

### 1. Executive Summary

MAGIC ERP is architected to operate on Windows retail workstations and cash counters via a native WPF desktop shell hosting the Angular single-page application inside Microsoft Edge WebView2. 

In strict adherence to Section 11 of the audit instructions, **compilation success on macOS does not constitute proof of Windows runtime execution.** This report evaluates what has been verified on the current build system, identifies architectural mechanisms designed for Windows, and details the specific release conditions and security signing requirements needed for Windows deployment.

---

### 2. Compilation & Architecture Status

#### A. Build Output
```bash
$ dotnet build 01-SRC/Desktop/Magic.Desktop.Wpf/Magic.Desktop.Wpf.csproj
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:00.54
```
- **Target Framework:** `net10.0-windows`
- **Output Assembly:** `Magic.Desktop.Wpf.dll`
- **Compilation Status:** **100% SUCCESS**

#### B. Component Architecture
1. **WPF Host Shell (`MainWindow.xaml`):** Hosts the WebView2 control, manages system tray lifecycle, and registers the JavaScript-to-C# native hardware bridge.
2. **WebView2 Runtime Integration:** Loads `http://127.0.0.1:5000` (or embedded Angular distribution in single-computer mode) with hardware acceleration enabled.
3. **Backend Host Lifecycle:** In standalone mode, the desktop shell launches the ASP.NET Core Kestrel backend as a managed child process, monitoring health via named pipes or localhost loopback and terminating it gracefully on application shutdown.

---

### 3. Thermal Receipt Printing & Hardware Bridge (DEF-DESK-001)

#### A. Native Win32 Spooler Implementation (`WindowsNativeHardwareBridge.cs`)
To replace the mock stub without introducing fragile third-party dependencies, `WindowsNativeHardwareBridge.cs` implements direct P/Invoke bindings to the core Windows Print Spooler (`winspool.drv`):
- `OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefault)`
- `StartDocPrinter(IntPtr hPrinter, int level, ref DOCINFO pDocInfo)`
- `StartPagePrinter(IntPtr hPrinter)`
- `WritePrinter(IntPtr hPrinter, byte[] pBytes, int dwCount, out int dwWritten)`
- `EndPagePrinter(IntPtr hPrinter)`
- `EndDocPrinter(IntPtr hPrinter)`
- `ClosePrinter(IntPtr hPrinter)`

#### B. ESC/POS Command Sequences Implemented
- **Initialize Printer:** `\x1B\x40` (`ESC @`)
- **Center Alignment:** `\x1B\x61\x01` (`ESC a 1`)
- **Left Alignment:** `\x1B\x61\x00` (`ESC a 0`)
- **Bold On / Off:** `\x1B\x45\x01` / `\x1B\x45\x00`
- **Partial Cut with Feed:** `\x1D\x56\x00` (`GS V 0`)
- **Cash Drawer Kick (Pin 2 Solenoid):** `\x1B\x70\x00\x19\xFA` (`ESC p 0 25 250`)
- **Cash Drawer Kick (Pin 5 Solenoid):** `\x1B\x70\x01\x19\xFA` (`ESC p 1 25 250`)

#### C. Environmental Disclaimer
> **[IMPORTANT] Physical Hardware Disclaimer:**  
> While the code is structurally complete, compiles with zero warnings, and contains browser-print fallback logic for non-spooler environments, **physical thermal paper printing, line feeds, cutter actuation, and cash drawer solenoid kicks cannot be physically tested on macOS without an attached physical USB/COM ESC/POS hardware device.** Physical validation is designated as a mandatory pre-deployment condition on an actual Windows staging lane.

---

### 4. Windows Security & Code Signing Verification

In strict compliance with the prompt directives:
> *"Do not disable, bypass or weaken Microsoft Defender or Windows Smart App Control. Clearly distinguish Windows Authenticode executable signing from MAGIC's Ed25519 application-update signature."*

#### A. Cryptographic Signature Distinctions
1. **Windows Authenticode Signing (Operating System Gate):**
   - **Purpose:** Satisfies Windows Smart App Control (SAC) and Windows Defender SmartScreen.
   - **Requirement:** Standard/EV Code Signing Certificate issued by a trusted public Certificate Authority (DigiCert, Sectigo, GlobalSign) or a trusted Enterprise Root CA via SignTool (`signtool sign /fd SHA256 /tr http://timestamp.digicert.com`).
   - **Critical Warning:** Unsigned executables will be aggressively blocked by Windows Smart App Control on Windows 11 workstations. **Authenticode signing must be applied prior to retail installer distribution.**
2. **MAGIC Application-Update Signing (Application Level Gate):**
   - **Purpose:** Cryptographically verifies delta/full automatic updates downloaded by the desktop client.
   - **Algorithm:** Asymmetric Ed25519 keypair. The private key signs update manifests and package hashes; the public key is embedded in `Magic.Desktop.Wpf` to verify payloads before extracting.

---

### 5. Windows Staging Verification Checklist

| Test Item | Verification Mechanism | Status on macOS Host | Required Staging Action |
|---|---|:---:|---|
| **WPF Compilation** | `dotnet build` | **VERIFIED (0 errors)** | None |
| **P/Invoke Win32 Signatures** | Code Review & Win32 spec | **VERIFIED (Valid)** | Verify under Windows runtime |
| **WebView2 Runtime** | Edge WebView2 Evergreen | **BLOCKED BY ENVIRONMENT** | Verify installation of WebView2 Evergreen runtime |
| **Physical ESC/POS Print** | Thermal Spooler | **BLOCKED BY ENVIRONMENT** | Test on physical Epson/TVS thermal printer |
| **Cash Drawer Kick** | Solenoid pulse via ESC/POS | **BLOCKED BY ENVIRONMENT** | Test physical drawer opening on cash checkout |
| **Smart App Control** | Authenticode Certificate | **ACTION REQUIRED** | Sign binary with official enterprise code signing key |

