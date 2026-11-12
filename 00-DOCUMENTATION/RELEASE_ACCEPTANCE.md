# MAGIC — RELEASE ACCEPTANCE GATE & SIGN-OFF CRITERIA

**Document Version:** 1.1.0  
**Status:** Phase 0 Planning Specification (Pending Review)  
**Last Updated:** 2026-11-12  

---

## 1. Release Philosophy

No version or milestone of MAGIC may be promoted to release without satisfying **100% of the mandatory Quality, Financial, and Security Gates**.

---

## 2. Mandatory Release Acceptance Checklist

### 2.1 Financial & Accounting Verification
- [ ] $\sum \text{Debit} \equiv \sum \text{Credit}$ verified across 100% of test database vouchers.
- [ ] Reverse inclusive GST calculations test suite passes with zero precision loss across 100,000 synthetic test cases.
- [ ] Section 170 CGST Act invoice rounding correctly accounts for fractional paise into the Rounding Off ledger.
- [ ] Trial balance nets to exact zero.
- [ ] Accounts Receivable subledger equals GL Control Account `1130`.
- [ ] Accounts Payable subledger equals GL Control Account `2110`.
- [ ] Inventory valuation matches GL Control Account `1140`.

### 2.2 Security & Integrity Verification
- [ ] All passwords verified to use Argon2id.
- [ ] Passwords and sensitive tokens verified absent from all API logs and client responses.
- [ ] Role-Based Access Control (RBAC) verified on all API endpoints; zero unauthenticated write mutations.
- [ ] Supervisor override authorization verified with audit logging.
- [ ] PostgreSQL port `5432` confirmed inaccessible from remote network interfaces.

### 2.3 Concurrency & Performance Verification
- [ ] High-concurrency checkout stress test passes without overselling or negative inventory.
- [ ] Idempotency verification confirms duplicate requests are absorbed without duplicate postings.
- [ ] POS barcode item lookup latency verified under 50ms.
- [ ] General Ledger query latency verified under 500ms for 100,000 journal records.

### 2.4 Operational & Deployment Verification
- [ ] Backend installs and runs successfully as an unattended Windows Service.
- [ ] WPF + WebView2 desktop container launches and renders without Chromium script errors.
- [ ] Automated backup creates valid `.dump` and passes test restore drill.
- [ ] Release binaries signed or documented for Windows Defender safe execution.
- [ ] Zero unapproved schema changes or undocumented API mutations.

