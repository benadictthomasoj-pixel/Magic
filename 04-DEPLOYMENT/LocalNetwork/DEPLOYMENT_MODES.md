# MAGIC — Phase 1 Deployment Foundation (Single-Computer & Local Network)

**Status:** Phase 1 Technical Foundation Specification & Templates  
**Verification Note:** Windows Service runtime, Windows Firewall PowerShell commands, and WPF/WebView2 runtime behavior are authored for Windows 10/11 (x64) and remain **Pending Windows Host Verification** while active development runs on macOS ARM64.

---

## Mode A — Single Computer (Standalone Counter / Office PC)

- **Components:**
  - `Magic.Desktop.Wpf` (Windows WPF shell hosting embedded WebView2)
  - Angular Frontend SPA served from `https://127.0.0.1:5001` (or bundled static assets served by local backend)
  - `Magic.Api` ASP.NET Core (.NET 10 LTS) running locally on `127.0.0.1:5001` (HTTPS) / `127.0.0.1:5000` (HTTP local dev)
  - Local PostgreSQL bound exclusively to `127.0.0.1:5432`
- **Database Access Rule:** Desktop WPF and WebView2 never connect directly to PostgreSQL; all operations pass through `Magic.Api`.

---

## Mode B — Local Network (Dedicated Windows Server + LAN Client Terminals)

- **Dedicated In-Store Server PC (Windows 10/11 or Windows Server):**
  - PostgreSQL bound strictly to `127.0.0.1:5432` (never listening on `0.0.0.0` or LAN IP).
  - `Magic.Api` hosted independently as a **Windows Service** (`MagicBackend`) via `Microsoft.Extensions.Hosting.WindowsServices`, listening on `https://<server-lan-ip>:5001`.
- **LAN Client Workstations:**
  - Run `Magic.Desktop.Wpf` + WebView2 configured with `ServerBaseUrl = https://<server-lan-ip>:5001`.
  - Send `X-Branch-Id`, `X-Terminal-Id`, `X-Correlation-Id`, and authorization credentials (subject to `OPEN-003` finalization) per API request.
  - Zero local PostgreSQL installation and zero SQLite offline buffering (`OPEN-001` Strict Online LAN confirmed).

---

## Security & Network Requirements Checklist

1. **HTTPS & Local CA Trust:**
   - Provision a dedicated internal server TLS certificate (self-signed local CA or AD CS / mkcert enterprise PKI) and install the Root CA in Windows `LocalMachine\Root` so WebView2 trusts the LAN API without insecure overrides.
   - Never bypass certificate validation in production builds (`OPEN-001` / `DEBT-001`).
2. **Windows Firewall Configuration (`Windows Defender Firewall`):**
   - Allow inbound TCP port `5001` on `Private` network profile only for `Magic.Api` (`Magic.Host.exe`), scoped to the local store subnet (e.g. `192.168.1.0/24`).
   - Block inbound TCP port `5432` (`PostgreSQL`) from all non-loopback interfaces.
3. **Terminal Registration Protocol (Boundary Prepared in Phase 1):**
   - Terminals register against an authoritative `BranchId` + `TerminalId`.
   - Administrators approve terminals server-side before POS/commercial operations are permitted.
4. **Server Availability Monitoring:**
   - Liveness endpoint: `GET /health`
   - Readiness endpoint (verifies PostgreSQL connectivity): `GET /health/ready`
   - Client terminals poll `/health/ready` via Angular connectivity monitor and fail closed with a clear operator alert when LAN/server drops (no unsafe offline commit buffering).

