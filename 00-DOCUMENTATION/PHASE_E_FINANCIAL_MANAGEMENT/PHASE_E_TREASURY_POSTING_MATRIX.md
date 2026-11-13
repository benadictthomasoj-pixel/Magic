# MAGIC ERP — PHASE E: TREASURY & FINANCIAL POSTING MATRIX

**Project:** MAGIC – Universal Business Management ERP  
**Phase:** E — Financial Management & Treasury  

---

## 1. Accounting Posting Matrix for Financial Transactions

Every operational financial event automatically creates an immutable, balanced `JournalVoucher` aggregate root in the General Ledger. All monetary values are integer paise (`BIGINT`).

| Event | Operational Origin | Voucher Type | Debit Account | Credit Account | Ledger Invariant |
|---|---|:---:|---|---|---|
| **Customer Receipt (Settlement)** | `/accounting/customer-receipts` | `Receipt` (5) | Bank Account (`1112`) or Cash (`1111`) | Trade Debtors Control (`1201`) | $\Delta \text{Bank} = +\$A, \Delta \text{Debtors} = -\$A$ |
| **Customer Advance Receipt** | `/accounting/customer-receipts` | `Receipt` (5) | Bank Account (`1112`) or Cash (`1111`) | Trade Debtors Control (`1201`) *(Advance tracked in subledger)* | $\Delta \text{Bank} = +\$A, \Delta \text{AR} = -\$A$ |
| **Customer Receipt Reversal** | `/accounting/customer-receipts` | `Reversal` (7) | Trade Debtors Control (`1201`) | Bank Account (`1112`) or Cash (`1111`) | Restores original invoice outstanding balance |
| **Supplier Bill Approval** | `/accounting/payables` | `Purchase` (3) | Commercial Purchases (`5101`) & Input GST (`1131`/`1132`) | Trade Creditors Control (`2111`) | $\text{Purchases} + \text{Tax} \equiv \text{Creditors}$ |
| **Supplier Payment (Settlement)** | `/accounting/supplier-payments` | `Payment` (4) | Trade Creditors Control (`2111`) | Bank Account (`1112`) or Cash (`1111`) | $\Delta \text{Creditors} = -\$A, \Delta \text{Bank} = -\$A$ |
| **Supplier Advance Payment** | `/accounting/supplier-payments` | `Payment` (4) | Trade Creditors Control (`2111`) | Bank Account (`1112`) or Cash (`1111`) | Advance tracked in subledger |
| **Supplier Payment Reversal** | `/accounting/supplier-payments` | `Reversal` (7) | Bank Account (`1112`) or Cash (`1111`) | Trade Creditors Control (`2111`) | Restores original bill payable balance |
| **Cash Deposit to Bank** | `/accounting/treasury` | `Contra` (6) | Bank Account (`1112`) | Cash on Hand (`1111`) | Net Cash & Bank Balance $\Delta = 0$ |
| **Cash Withdrawal from Bank** | `/accounting/treasury` | `Contra` (6) | Cash on Hand (`1111`) | Bank Account (`1112`) | Net Cash & Bank Balance $\Delta = 0$ |
| **Inter-Bank Transfer** | `/accounting/treasury` | `Contra` (6) | Destination Bank (`1112` or specific GL) | Source Bank (`1112` or specific GL) | Net Cash & Bank Balance $\Delta = 0$ |
| **Bank Charges** | `/accounting/treasury` / BRS | `Journal` (1) | Administrative Bank Charges (`5201`) | Bank Account (`1112`) | Expense recognized, bank deducted |
| **Bank Interest Credit** | `/accounting/treasury` / BRS | `Receipt` (5) | Bank Account (`1112`) | Other Revenue / Interest Income (`4101`) | Income recognized, bank increased |

---

## 2. Subledger Integrity & Reconciliation Formula

To ensure `INV-FIN-04` (Dual Control AR/AP Reconciliation with General Ledger):

$$\text{Balance of GL 1201 (Trade Debtors)} \equiv \sum_{\text{OPEN, PARTIAL}} \text{ar\_subledger\_items.outstanding\_amount\_paise} - \sum_{\text{SUBMITTED}} \text{customer\_receipts.unallocated\_amount\_paise}$$

$$\text{Balance of GL 2111 (Trade Creditors)} \equiv \sum_{\text{OPEN, PARTIAL}} \text{ap\_subledger\_items.outstanding\_amount\_paise} - \sum_{\text{SUBMITTED}} \text{supplier\_payments.unallocated\_amount\_paise}$$
