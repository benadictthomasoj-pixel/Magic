# MAGIC — DEPLOYMENT, INFRASTRUCTURE & WINDOWS SECURITY GUIDE

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  

---

## 1. Deployment Topologies

MAGIC is built to operate reliably in local-first zero-cloud environments across two primary architectures:

```
MODE A: STANDALONE SINGLE-WORKSTATION
+----------------------------------------------------------------------------------------------------+
| SINGLE WINDOWS 10 / 11 PC                                                                          |
|   [WPF Desktop Application + WebView2 Angular Frontend]                                           |
|                           | (Localhost HTTP/HTTPS)                                                 |
|   [ASP.NET Core Backend Process / In-Memory Host]                                                  |
|                           | (127.0.0.1:5432 TCP)                                                   |
|   [PostgreSQL 16 Database Service]                                                                 |
+----------------------------------------------------------------------------------------------------+

MODE B: LOCAL AREA NETWORK (LAN) IN-STORE SERVER + CLIENT TERMINALS
+----------------------------------------------------------------------------------------------------+
| IN-STORE DEDICATED SERVER PC                                                                       |
|   - PostgreSQL 16 Service (Bound to 127.0.0.1)                                                     |
|   - ASP.NET Core Backend (Windows Service, Listening on https://192.168.1.50:5001)                |
|   - Automated Hourly PostgreSQL Backup Task                                                        |
+----------------------------------------------------------------------------------------------------+
        ^                                                   ^
        | (Encrypted TLS API Calls)                         | (Encrypted TLS API Calls)
+-------------------------------------+   +-------------------------------------+
| CLIENT TERMINAL 1 (Counter POS)     |   | CLIENT TERMINAL 2 (Back-Office)     |
|   - WPF Shell + WebView2 UI         |   |   - WPF Shell + WebView2 UI         |
|   - Local Receipt Printer (USB)     |   |   - Standard A4 Laser Printer       |
|   - Barcode Scanner (HID)           |   |   - No Local DB Required            |
+-------------------------------------+   +-------------------------------------+
```

---

## 2. Windows Service Hosting Architecture

The backend is engineered to run as a native **Windows Service** independently of whether any user is logged into the server desktop.

### Windows Service Registration
Using standard .NET `Microsoft.Extensions.Hosting.WindowsServices`:
```powershell
# Create Windows Service via PowerShell (Administrator)
New-Service -Name "MagicBackend" `
            -BinaryPathName "C:\Program Files\MAGIC\Backend\Magic.Host.exe --service" `
            -DisplayName "MAGIC ERP Backend Host" `
            -StartupType Automatic `
            -Description "Authoritative business logic, financial engine and API gateway for MAGIC ERP."

# Configure Recovery on Failure
sc.exe failure "MagicBackend" reset= 86400 actions= restart/60000/restart/60000/none/0
```

---

## 3. Automated Backup, Verification & Recovery Protocol

Data protection is an existential requirement for business ERPs.

### Automated Backup Pipeline
1. **Scheduled Execution:**
   - A background worker triggers automated daily and hourly snapshots using native `pg_dump`:
     ```powershell
     pg_dump -h 127.0.0.1 -p 5432 -U magic_admin -F c -b -v -f "C:\MAGIC\Backups\magic_backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').dump" magic_db
     ```
2. **Integrity Check & SHA-256 Hashing:**
   - Immediately following dump completion, a SHA-256 checksum file is generated.
3. **Automated Restore Testing:**
   - On a weekly schedule, the system performs a non-destructive test restore into a temporary database `magic_verify_test`, runs a verification query against the General Ledger, and drops the test database.
4. **Retention Policy:**
   - Keep 24 hourly snapshots, 30 daily snapshots, and 12 monthly closing snapshots.

---

## 4. Windows Security, Defender & Authenticode Signing

### 4.1 Authenticode Executable Signing
- Distribute binaries signed with a trusted Sectigo / DigiCert EV (Extended Validation) or Standard Code Signing Certificate to prevent Windows SmartScreen blocks.
- Where an EV certificate is cost-prohibitive for initial internal testing, document explicit installation steps to add the publisher certificate to the enterprise's **Trusted Root Certification Authorities** store.
- **Strict Rule:** Never instruct users to disable Windows Defender or bypass Smart App Control.

### 4.2 Application-Level Cryptographic Signatures
- Separate from Windows Authenticode binary signing, internal software update packages and terminal authentication tokens are validated using **Ed25519 cryptographic signatures**.
- The desktop shell verifies update packages against the vendor public key before unpacking to eliminate tampering.


---

## Phase 1 Local-First Deployment Templates & Platform Status (2026-11-12)

- **Added Templates & Scripts:**
  - `04-DEPLOYMENT/LocalNetwork/DEPLOYMENT_MODES.md` — Documents Mode A (Single Computer) and Mode B (Dedicated LAN Server + LAN Terminals) with strict zero PostgreSQL LAN exposure boundaries.
  - `04-DEPLOYMENT/LocalNetwork/appsettings.Host.Template.json` — Secret-free configuration skeleton requiring externalized environment variables or Windows Secret Manager for `ConnectionStrings:MagicDatabase`.
  - `04-DEPLOYMENT/Windows/Register-MagicWindowsService.ps1` — PowerShell sc/New-Service helper for registering `Magic.Api.exe` as a Windows Service (`MagicBackend`).
- **macOS ARM64 Build vs Windows Runtime Boundary:**
  - `Magic.Api` and `Magic.Frontend` built and verified natively on macOS ARM64 (`arm64`).
  - `Magic.Desktop.Wpf` (`net10.0-windows`) compiled cleanly on macOS via `EnableWindowsTargeting=true`, while live WPF desktop execution, Microsoft WebView2 Edge runtime initialization, peripheral hardware drivers, and Windows Service execution remain **Pending Windows 10/11 Verification**.
