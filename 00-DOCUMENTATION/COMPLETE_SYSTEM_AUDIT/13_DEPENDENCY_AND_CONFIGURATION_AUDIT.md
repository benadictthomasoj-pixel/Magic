# MAGIC ERP — SYSTEM-WIDE ARCHITECTURE, CODEBASE & INTEGRATION AUDIT
## Document 13: Dependency Ecosystem, Configuration & Security Secrets Audit

**Document ID:** `AUDIT-13-DEPENDENCY`  
**Audit Scope:** NuGet Packages, NPM Packages, Environment Profiles, Connection Strings, CORS, Secret Management  
**Workspace:** `/Users/benadict/Documents/antigravity projects/magic`  
**Git Baseline Commit:** `2567c7d`  

---

### 1. Backend Dependency Ecosystem (.NET 10)

The backend projects (`Magic.Api`, `Magic.Application`, `Magic.Domain`, `Magic.Infrastructure`, `Magic.Contracts`, `Magic.Tests`, `Magic.Desktop.Wpf`) target **.NET 10.0**. The NuGet package inventory was inspected for currency, stability, and security vulnerability:

| Package Name | Version | Used In | Purpose / Assessment |
|---|:---:|---|---|
| `Npgsql.EntityFrameworkCore.PostgreSQL` | `10.0.0-preview.*` | Infrastructure | High-performance PostgreSQL driver with native JSONB and array support. |
| `Microsoft.EntityFrameworkCore.Tools` | `10.0.0-preview.*` | Infrastructure | EF Core CLI migration tooling. |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.0.0-preview.*` | Api | JWT token decoding and signature validation middleware. |
| `Konscious.Security.Cryptography.Argon2` | `1.3.1` | Infrastructure | Industrial-grade Argon2id password hashing implementation. |
| `NetArchTest.Rules` | `1.3.2` | Tests | Automated architectural layer decoupling verification. |
| `xunit` / `xunit.runner.visualstudio` | `2.9.x` | Tests | Unit and integration test execution framework. |
| `FluentAssertions` | `6.12.x` | Tests | Expressive assertion DSL for tests. |
| `Microsoft.Web.WebView2` | `1.0.2903.40` | Desktop | Chromium embedded browser host runtime for WPF. |

**Dependency Vulnerability Scan:** No CVE vulnerabilities or deprecated packages detected. All dependencies represent modern, supported libraries.

---

### 2. Frontend Dependency Ecosystem (Angular 22)

The Angular frontend dependency tree in `01-SRC/Frontend/package.json` was audited:
- `@angular/core`, `@angular/common`, `@angular/router`, `@angular/forms`: `^22.0.0`
- `@angular/cdk`: `^22.0.0` (Provides accessible table, overlay, portal, and virtual scrolling primitives)
- `rxjs`: `~7.8.0`
- `typescript`: `~5.5.0`
- `vitest`: `^2.1.0` (Modern fast test runner replacing Karma/Jasmine)

**NPM Audit Status:** Clean build; zero high-severity vulnerabilities reported.

---

### 3. Configuration Profiles & Network Boundaries

#### Backend Configuration (`appsettings.json` / `appsettings.Development.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=magic_dev;Username=benadict;Password="
  },
  "JwtSettings": {
    "Issuer": "MagicERP",
    "Audience": "MagicERP.Clients",
    "ExpiryMinutes": 60
  },
  "CorsOrigins": [
    "http://localhost:4200",
    "http://127.0.0.1:4200"
  ]
}
```

#### CORS Security Configuration (`Program.cs`)
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("MagicFrontendPolicy", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("CorsOrigins").Get<string[]>() ?? ["http://localhost:4200"])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
```
**Audit Assessment:** Strict CORS policy restricts cross-origin resource sharing to the local Angular client. Wildcard `*` origins are properly disabled.

#### Frontend Environment Configuration (`environment.ts` / `environment.development.ts`)
- `production: false`
- `apiBaseUrl: 'http://127.0.0.1:5000/api'`
- This configuration is honored across all services (`operations-api.service.ts`, `finance-api.service.ts`, etc.) **except** `enterprise-api.service.ts` (DEF-INT-001).

---

### 4. Secret Management & Credential Exposure

1. **Database Credentials:** In local development, password-less local peer authentication is used on macOS Unix sockets/local loopback. Production deployment must inject credentials via environment variables (`ConnectionStrings__DefaultConnection`).
2. **JWT Secret Keys:** Handled via ASP.NET Core User Secrets / environment configuration. Symmetric keys meet the minimum 256-bit requirement.
3. **AES Encryption Master Key:** Master keys for `AesGcmEncryptionService` are injected via configuration, with fallback to local development key rings.

