# MAGIC ERP — PHASE 2, STAGE 4 API CONTRACTS
## RESTful Endpoints, Request/Response Payloads & HTTP Headers

| Attribute | Value |
| :--- | :--- |
| **API Version** | `v1` |
| **Base URI** | `/api/v1/accounting` |
| **Protocol** | Strict Online LAN (HTTP/1.1 & HTTP/2) |
| **Authentication Policy** | Fail-Closed Authenticated Principal (`OPEN-003`) |
| **Encoding** | JSON (`application/json`) |

---

## 1. Global Request Headers

| Header | Type | Required | Description |
| :--- | :--- | :--- | :--- |
| `X-Company-Id` | UUID | Optional | Target corporate legal entity ID (defaults to default company if omitted). |
| `X-Branch-Id` | UUID | Optional | Operating branch identifier (defaults to Main branch if omitted). |
| `X-Correlation-Id` | UUID | Optional | Client trace correlation ID propagated to database audit records. |
| `Authorization` | String | Conditional | `Bearer <token>` required for all financial mutation endpoints (`POST`, `PUT`). |

---

## 2. API Endpoint Specifications

### 2.1 Accounting Dashboard
- **`GET /api/v1/accounting/dashboard`**
  - **Description**: Returns consolidated KPI metrics, system validation state, and recent voucher summaries.
  - **Success Response (200 OK)**:
    ```json
    {
      "success": true,
      "data": {
        "totalDebitPaise": 0,
        "totalCreditPaise": 0,
        "postedVouchersCount": 0,
        "draftVouchersCount": 0,
        "activeFiscalYearName": "Financial Year 2026-2027",
        "activeFiscalPeriodCode": "2026-11",
        "isActivePeriodLocked": false,
        "recentVouchers": [],
        "systemValidationStatus": "EMPTY"
      },
      "correlationId": "01a1cf0a-44bf-707c-b571-a4e0572e075d",
      "timestampUtc": "2026-11-12T09:15:19.107202Z"
    }
    ```

---

### 2.2 Chart of Accounts (COA)
- **`GET /api/v1/accounting/account-groups`**
  - **Description**: Lists account group taxonomy (Assets, Liabilities, Equity, Revenue, Expenses).
  - **Response**: Array of `AccountGroupDto` (`id`, `parentGroupId`, `code`, `name`, `rootClass`, `hierarchyDepth`, `isSystem`, `isActive`).

- **`GET /api/v1/accounting/accounts`**
  - **Query Parameters**: `rootClass` (string), `activeOnly` (boolean), `search` (string).
  - **Response**: Array of `AccountDto` (`id`, `groupId`, `groupName`, `code`, `name`, `rootClass`, `currencyCode`, `isActive`, `controlAccountType`, `currentBalancePaise`).

- **`POST /api/v1/accounting/accounts`**
  - **Security**: Requires authenticated user identity (`OPEN-003`).
  - **Request Body**:
    ```json
    {
      "groupId": "01a1cefa-db7f-7d55-a10a-8042afaa407a",
      "code": "1115",
      "name": "Petty Cash Imprest",
      "rootClass": "Asset",
      "controlAccountType": null
    }
    ```
  - **Response**: `201 Created` with created `AccountDto`.

- **`PUT /api/v1/accounting/accounts/{id}`**
  - **Security**: Requires authenticated user identity (`OPEN-003`).
  - **Request Body**: `{ "name": "Petty Cash - Main Office", "isActive": true }`

---

### 2.3 Journal Vouchers & Transactions
- **`GET /api/v1/accounting/vouchers`**
  - **Query Parameters**: `dateFrom`, `dateTo`, `voucherType`, `status`, `search`, `page`, `pageSize`.
  - **Response**: Paginated list of `VoucherSummaryDto`.

- **`GET /api/v1/accounting/vouchers/{id}`**
  - **Response**: `VoucherDetailDto` including lines with integer-paise amounts, party tracking, and audit timestamps.

- **`POST /api/v1/accounting/vouchers/draft`**
  - **Security**: Requires authenticated user identity (`OPEN-003`).
  - **Request Body**:
    ```json
    {
      "fiscalPeriodId": "01a1cefa-db7f-7123-b123-0123456789ab",
      "voucherType": "Journal",
      "voucherDate": "2026-11-12",
      "narration": "Transfer from Bank to Cash account",
      "referenceNumber": "TRF-0012",
      "lines": [
        { "accountId": "01a1cefa-db84-773e-b078-c5b7211fac34", "debitPaise": 500000, "creditPaise": 0, "narration": "Cash received" },
        { "accountId": "01a1cefa-db84-74ab-b705-4e1febe09e9b", "debitPaise": 0, "creditPaise": 500000, "narration": "Bank withdrawal" }
      ]
    }
    ```

- **`POST /api/v1/accounting/vouchers/validate`**
  - **Description**: Validates voucher double-entry invariants without persisting to database.
  - **Response**:
    ```json
    {
      "success": true,
      "data": {
        "isValid": true,
        "totalDebitPaise": 500000,
        "totalCreditPaise": 500000,
        "differencePaise": 0,
        "isBalanced": true,
        "errors": [],
        "warnings": []
      }
    }
    ```

- **`POST /api/v1/accounting/vouchers/{id}/post`**
  - **Security**: Requires authenticated user identity (`OPEN-003`).
  - **Description**: Executes ACID posting with advisory lock, assigns chronological voucher number, and updates General Ledger.

- **`POST /api/v1/accounting/vouchers/{id}/reverse`**
  - **Security**: Requires authenticated user identity (`OPEN-003`).
  - **Request Body**: `{ "branchCode": "MAIN", "fyCode": "FY26", "reason": "Correction of duplicate transaction" }`
  - **Description**: Creates and posts a compensating reversal voucher.

---

### 2.4 General Ledger & Trial Balance
- **`GET /api/v1/accounting/general-ledger`**
  - **Query Parameters**: `accountId` (required UUID), `dateFrom` (ISO date), `dateTo` (ISO date).
  - **Response**: `GeneralLedgerReportDto` with `openingBalancePaise`, `closingBalancePaise`, and chronological `entries`.

- **`GET /api/v1/accounting/trial-balance`**
  - **Query Parameters**: `asOfDate` (ISO date).
  - **Response**: `TrialBalanceReportDto` with account rows, debit/credit totals, and `isBalanced` invariant flag.

---

### 2.5 Fiscal Governance
- **`GET /api/v1/accounting/fiscal-years`**: Lists fiscal years and their 12 calendar periods.
- **`POST /api/v1/accounting/fiscal-periods/{id}/lock`**: Freezes period from further postings.
- **`POST /api/v1/accounting/fiscal-periods/{id}/close`**: Initiates period closing.
- **`POST /api/v1/accounting/fiscal-periods/{id}/reopen`**: Reopens closed/locked period.

---

### 2.6 Subledgers
- **`GET /api/v1/accounting/subledgers/ar`**: Accounts Receivable control account status (`11300`).
- **`GET /api/v1/accounting/subledgers/ap`**: Accounts Payable control account status (`21100`).
