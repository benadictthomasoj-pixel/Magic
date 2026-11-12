# MAGIC — COMPREHENSIVE TESTING STRATEGY & QUALITY PROTOCOLS

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  

---

## 1. Quality Mandate: Test Verification Principle

**Core Rule:** No feature or module may be declared complete merely because a user interface screen appears to render or an API returns a `200 OK` on a single happy-path test.

Financial software requires rigorous, deterministic verification across all edge cases, race conditions, and precision limits.

---

## 2. Multi-Tiered Testing Pyramid

```
                                 / \
                                /   \
                               / E2E \       <-- Playwright (WebView2 CDP) + WPF UI Automation (FlaUI)
                              /-------\
                             / Concurr \     <-- Multi-terminal race conditions, oversell tests
                            /-----------\
                           / Integration \   <-- Testcontainers PostgreSQL + EF Core 10 API integration
                          /---------------\
                         / Financial Tests \ <-- GST reverse math, odd-cent split, double-entry balance
                        /-------------------\
                       /     Unit Tests      \ <-- Pure domain logic, value objects, invariants (xUnit)
                      /-----------------------\
```

---

## 3. Test Suites & Protocols

### 3.1 Unit Testing (C# xUnit / FluentAssertions)
- **Scope:** Pure domain models, entities, value objects (`Money`, `Quantity`, `TaxRate`), and domain services.
- **Rules:** 100% in-memory execution; zero database or network dependencies; execution time < 10ms per test.
- **Key Assertions:**
  - `Money.FromPaise(100)` + `Money.FromPaise(200)` = `Money.FromPaise(300)`.
  - Negative quantities rejected where invariants forbid them.
  - Barcode and SKU formatting validators.

### 3.2 Financial & Tax Integrity Testing
- **Test Scenarios:**
  - **Reverse Inclusive GST Precision:** Test 100,000 synthetic gross prices with varied GST rates (0%, 5%, 12%, 18%, 28%) to assert:
    $$\text{TaxablePaise} + \text{CGSTPaise} + \text{SGSTPaise} \equiv \text{GrossPaise}$$
    Zero floating-point drift, zero dropped single paise.
  - **Odd-Cent Tax Head Split:** Odd paise GST values split deterministically without losing 1 paise.
  - **Double-Entry Balance Verification:** Every created journal voucher must pass:
    $$\sum \text{DebitLines} - \sum \text{CreditLines} = 0$$
  - **Section 170 Rounding Off:** Invoices rounded off to nearest whole rupee with delta logged to Rounding Account.

### 3.3 Concurrency & Race-Condition Testing
- **Multi-Terminal POS Oversell Test:**
  - 5 simulated terminal threads concurrently submit checkout requests for an item with a stock balance of exactly 5 units, each requesting 2 units (Total demand = 10 units).
  - **Expected Outcome:** Exactly 2 transactions succeed; 3 transactions fail with `InsufficientStockException`. Stock balance reaches exactly 1 unit (or 0 if 3 units). The inventory balance must never drop below zero.
- **Idempotent Duplicate Billing Test:**
  - Submit the identical `Idempotency-Key` across 10 simultaneous HTTP threads.
  - **Expected Outcome:** Exactly 1 invoice created; 9 requests receive the cached successful response without duplicate stock decrements or duplicate accounting vouchers.

### 3.4 Integration Testing (Testcontainers for .NET)
- Uses ephemeral PostgreSQL 16 Docker containers spun up dynamically during test execution.
- Tests verify actual EF Core migrations, Npgsql binary queries, schema boundaries, and database constraints.
- **Strict Isolation:** Tests never run against production or long-lived shared databases.

### 3.5 Security & Privilege Escalation Testing
- Tests assert that an operator with `pos:checkout` cannot execute `accounting:periods:lock` or `admin:users:create`.
- Tests verify that supervisor override PIN validation fails when an invalid PIN is supplied.


---

## Phase 1 Verified Test Execution Results (Remediated — 2026-11-12)

Executed in Release configuration on macOS ARM64 (`.NET 10.0.403`, Node `v26.5.0`, Angular `22.2.6`, Vitest `5.1.0`) against isolated PostgreSQL `18.6` (`magic_dev`):

| Test Suite | Location | Outcome | Scope Verified |
|---|---|---|---|
| Domain Unit Tests (`Magic.Domain.UnitTests`) | `03-TESTS/Unit/Magic.Domain.UnitTests/` | **7 / 7 Passed** | 64-bit `Money` arithmetic/overflow, JSON scalar int64 serialization, decimal bankers rounding conversion, `DomainGuard`, `DomainEntity` UUIDv7 & domain events |
| Architecture Boundary Tests (`Magic.ArchitectureTests`) | `03-TESTS/Architecture/Magic.ArchitectureTests/` | **4 / 4 Passed** | Clean Architecture dependency rules (`Contracts`, `Domain`, `Application`) + zero SQLite/Quartz references |
| Backend & DB Integration Tests (`Magic.Api.IntegrationTests`) | `03-TESTS/Integration/Magic.Api.IntegrationTests/` | **14 / 14 Passed** | `/health`, PostgreSQL `/health/ready`, `/api/v1/system/status`, `/api/v1/system/context`, OpenAPI `/openapi/v1.json`, Audit Log Immutability trigger (`UPDATE`/`DELETE` rejection with `55000`), Idempotency cache replay & 422 mismatch rejection, Outbox `FOR UPDATE SKIP LOCKED` concurrency & consumer retry tracking |
| Frontend Unit Tests (`Vitest` / Angular TestBed) | `01-SRC/Frontend/src/app/**/*.spec.ts` | **9 / 9 Passed (6 files)** | `ThemeService` dark/light meta state, `AuthBoundaryService` (`OPEN-003` fail-closed), `correlationInterceptor` (including `Idempotency-Key` header contract), `ConnectivityMonitorService` strict LAN state, `ShellComponent`, `App` |
| WPF Desktop + E2E UI Tests | `03-TESTS/Desktop/` | **Pending Windows 10/11 Verification** | Live WPF/WebView2 rendering, bridge runtime, and FlaUI/Playwright E2E cannot run on macOS ARM64 (marked pending Windows testbed) |
