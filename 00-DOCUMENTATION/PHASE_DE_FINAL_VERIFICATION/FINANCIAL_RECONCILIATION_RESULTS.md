# MAGIC ERP — Financial Reconciliation Verification Results
## Mathematical and Subledger Verification

**Audit Scope:** Double-Entry Equilibrium, Subledger Control Reconciliations, and Cash Drawer Equations  

---

## 1. Double-Entry Parity ($\sum \text{Debit} \equiv \sum \text{Credit}$)

For every transaction processed across POS Checkout, Credit Sales, Sales Returns, Invoice Cancellations, Supplier Bills, and Customer Receipts, the general ledger vouchers maintain mathematical equilibrium:

### POS Retail Cash Checkout (Example)
$$\text{Debit: Account 1111 (Cash)} = 295,000 \text{ paise}$$
$$\text{Credit: Account 4101 (Revenue)} = 250,000 \text{ paise}$$
$$\text{Credit: Account 2121 (Output CGST)} = 22,500 \text{ paise}$$
$$\text{Credit: Account 2122 (Output SGST)} = 22,500 \text{ paise}$$
$$\Delta = \sum \text{Debit} - \sum \text{Credit} = 295,000 - 295,000 = 0 \quad (\text{Balanced})$$

### Customer Credit Sale Checkout (Example)
$$\text{Debit: Account 1111 (Cash Tender)} = 100,000 \text{ paise}$$
$$\text{Debit: Account 1201 (Trade Debtors)} = 195,000 \text{ paise}$$
$$\text{Credit: Account 4101 (Revenue)} = 250,000 \text{ paise}$$
$$\text{Credit: Account 2121 (Output CGST)} = 22,500 \text{ paise}$$
$$\text{Credit: Account 2122 (Output SGST)} = 22,500 \text{ paise}$$
$$\Delta = (100,000 + 195,000) - (250,000 + 22,500 + 22,500) = 295,000 - 295,000 = 0 \quad (\text{Balanced})$$

---

## 2. AR Subledger Control Parity
For each credit transaction:
$$\text{OriginalAmountPaise} \equiv \text{SettledAmountPaise} + \text{OutstandingAmountPaise}$$
$$\text{Trade Debtors GL Debit (Account 1201)} \equiv \sum \text{OutstandingAmountPaise (AR Subledger)}$$
Both relations are enforced at the database level by PostgreSQL check constraint `chk_ar_amounts` and verified by integration tests.

---

## 3. Cash Drawer Reconciliation Equation
$$\text{ExpectedCashPaise} = \text{OpeningCashPaise} + \text{CashSalesPaise} - \text{CashRefundsPaise} - \text{CashPayoutsPaise}$$
- Credit notes, card tenders, and UPI transactions have **zero** impact on $\text{CashRefundsPaise}$ or $\text{ExpectedCashPaise}$.
- Invoice cancellations deduct **only** actual physical cash tendered.
- The physical cash drawer equation is preserved exactly.

---

## 4. Indian Statutory GST Partitioning
- **Intra-State:** Tax amount is split identically into $50\%$ CGST (Account `2121` for output, Account `1131` for input) and $50\%$ SGST (Account `2122` for output, Account `1132` for input).
- **Inter-State:** Tax amount is routed $100\%$ to IGST (Account `2123` for output, Account `1133` for input) with exactly $0$ paise in CGST and SGST accounts.
- Statutory Rounding Note: Architectural note `DEC-ACC-003` governs invoice-level vs. line-level rounding.
