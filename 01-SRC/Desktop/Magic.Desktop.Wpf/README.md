# MAGIC Desktop Shell (`Magic.Desktop.Wpf`)

**Target Framework:** `net10.0-windows` (WPF + Microsoft Edge WebView2)  
**Host Verification Status:** **Pending Windows 10/11 Verification**

## Platform Scope Note
- During Phase 1 development on **macOS ARM64**, this project establishes the clean WPF + WebView2 solution structure, hardware bridge abstraction (`IDesktopHardwareBridge`), and shared bridge contracts via `Magic.Contracts`.
- Live runtime execution, Microsoft Edge WebView2 initialization, Windows Credential Locker integration, and FlaUI E2E tests **cannot run on macOS** and remain pending verification on an actual Windows 10/11 (x64) workstation.

