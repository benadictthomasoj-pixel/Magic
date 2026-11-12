# MAGIC ERP — WINDOWS DESKTOP RELEASE READINESS
## Desktop Host, Hardware Interfacing, Security & Packaging Specification

**Document Version:** 2.0.0  
**Project:** MAGIC – Universal Business Management ERP  
**Desktop Target:** Windows 10 (Build 19041+) and Windows 11 (x64 / ARM64)  
**Host Framework:** WPF (.NET 10 LTS) + Microsoft Edge WebView2  
**Hardware Bridge:** ESC/POS Thermal Printing, USB HID Barcode Scanners, Cash Drawers  
**Last Updated:** 2026-11-12  

---

## 1. Desktop Host Architecture (`Magic.Desktop.Wpf`)

MAGIC ERP utilizes a modern hybrid architecture:
- **Presentation Layer:** High-performance Chromium web rendering via **Microsoft Edge WebView2**, hosting the Angular 22 Single Page Application.
- **Native Host:** A lightweight **WPF (.NET 10 LTS)** wrapper providing access to local hardware devices and Windows OS APIs that are inaccessible from a standard web browser.
- **Communication Bridge:** Bidirectional asynchronous messaging via `CoreWebView2.WebMessageReceived` and `PostWebMessageAsJson()`, using strongly typed contracts defined in `Magic.Contracts.Desktop`.

```
┌────────────────────────────────────────────────────────┐
│              Windows 10 / 11 Desktop Host              │
│                                                        │
│  ┌──────────────────────────────────────────────────┐  │
│  │     Angular 22 Single Page Application (SPA)     │  │
│  │   [ POS ]  [ Invoicing ]  [ Ledgers ]  [ BI ]    │  │
│  └────────────────────────▲─────────────────────────┘  │
│                           │ PostWebMessage             │
│  ┌────────────────────────▼─────────────────────────┐  │
│  │    Microsoft Edge WebView2 Runtime (Chromium)    │  │
│  └────────────────────────▲─────────────────────────┘  │
│                           │ Native Interop             │
│  ┌────────────────────────▼─────────────────────────┐  │
│  │       WPF Host (.NET 10 C# Native Runtime)       │  │
│  │                                                  │  │
│  │  • Thermal ESC/POS Driver   • Cash Drawer RJ11   │  │
│  │  • USB HID Barcode Listener • Offline LAN Ping   │  │
│  └──────────────────────────────────────────────────┘  │
└────────────────────────────────────────────────────────┘
```

---

## 2. Hardware Interfacing Specification

### 2.1. Thermal Receipt Printing (ESC/POS)
- **Supported Widths:** Standard 80mm (48 characters/line) and compact 58mm (32 characters/line).
- **Communication Channels:** Windows Raw Print Spooler (`Winspool.drv`), USB Virtual COM, or LAN Network Print (Port 9100).
- **Command Set:** Pure ESC/POS binary command stream (`ESC @` initialize, `ESC a` alignment, `ESC E` bold, `GS V` paper cut).
- **Format:** Generates crisp text receipts with tax breakdown, QR code (UPI dynamic payment or B2B IRN e-Invoice), and store branding without browser print dialog popups.

### 2.2. Cash Drawer Kick Pulse
- **Interface:** RJ-11 / RJ-12 connector wired through receipt printer DK port.
- **Trigger Command:** `ESC p 0 25 250` (Pin 2, 50ms on, 500ms off pulse).
- **Execution:** Triggered automatically upon cash sale completion or manual supervisor cash drop.

### 2.3. Barcode Scanners
- **Device Support:** 1D / 2D USB HID Keyboard Wedge scanners and Serial/Virtual COM scanners.
- **Handling:** Configurable prefix/suffix detection (e.g. `STX ... ETX` or rapid keystroke bursts < 30ms between characters) to route scanned barcodes directly to the active POS item input without requiring manual text field focus.

---

## 3. Windows Security, Code Signing & Smart App Control

### 3.1. Code Signing Strategy
1. **Authenticode Binary Signing:**
   - For internal enterprise/LAN deployments: An internal Enterprise Root Certificate Authority (CA) or self-signed code-signing certificate installed in the Local Machine `Trusted Root Certification Authorities` and `Trusted Publishers` store.
   - For commercial distribution: Standard EV (Extended Validation) or OV Authenticode Certificate.
2. **Ed25519 Auto-Update Signature:**
   - Independent of Windows Authenticode, the application verifies the cryptographic signature of all update packages using an embedded Ed25519 public key before applying any update payload.

### 3.2. Windows Smart App Control & Microsoft Defender Guidance
- **Smart App Control (SAC):** Windows 11 feature that blocks unsigned or low-reputation applications.
- **Deployment Rule:** Unsigned binaries will trigger Smart App Control or SmartScreen warnings. Under no circumstances should installation instructions advise users to disable Smart App Control or disable Microsoft Defender globally.
- **Remediation Protocol:** 
  1. Sign binaries with an Authenticode certificate trusted on the corporate network.
  2. For pilot / dev testing on dedicated workstations, add an explicit application folder path exclusion in Windows Defender rather than disabling real-time security.

---

## 4. Packaging and Distribution Architecture

- **Packager:** Inno Setup / WiX Toolset generating a single-executable installer (`MagicSetup.exe`) or MSI package (`MagicInstaller.msi`).
- **Prerequisites:**
  - .NET 10 Desktop Runtime (bundled or web-downloaded).
  - Microsoft Edge WebView2 Evergreen Runtime (pre-installed on Windows 10/11).
  - PostgreSQL 18 Client Libraries / LAN connection to database host.
- **Silent Deployment:** Supports `/VERYSILENT /SUPPRESSMSGBOXES /NORESTART` command-line flags for enterprise IT automated deployment via Intune or Group Policy.

