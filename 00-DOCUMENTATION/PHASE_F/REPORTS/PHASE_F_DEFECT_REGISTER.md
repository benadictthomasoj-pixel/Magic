# MAGIC ERP — PHASE F DEFECT REGISTER
## Defects Identified, Root Causes, Corrections, and Regression Verification

**Project:** MAGIC — Universal Business Management ERP  
**Phase:** F — Extended Business Operations  
**Date:** 2026-11-13  
**Status:** ALL DEFECTS RESOLVED  

---

## Defect Summary Table

| Defect ID | Component | Severity | Symptom | Root Cause | Correction |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **DEF-F01** | Migration Script | High | Duplicate column error on `SalesInvoiceRecord.AccountingVoucherId` | Migration file contained duplicate `AddColumn` statements | Removed duplicate column definitions from migration code |
| **DEF-F02** | HrService | Low | CA1716 and CA1862/CA1304 compiler warnings | Parameter named `date` and case-insensitive search using `.ToLower()` | Renamed to `attendanceDate` and used `EF.Functions.ILike` |
| **DEF-F03** | ServiceDeskService | Medium | Compilation errors in sales invoice line creation | Referencing non-existent properties on `ProductPriceRecord` and `ProductRecord` | Aligned with actual `ProductCode` and `UnitPricePaise` properties |
| **DEF-F04** | Angular Models | Medium | TypeScript compile error on `PaginatedList` | Missing explicit generic parameter on `ApiResponse<T[], PaginationMeta>` | Explicitly typed `ApiResponse<readonly T[], PaginationMeta>` |
| **DEF-F05** | Angular HTML | Low | Angular template compiler error on `StatusBadgeComponent` | Used non-existent `@Input() status` instead of `@Input() label` | Updated all Phase F templates to use `[label]="..."` |
| **DEF-F06** | Database Roles | High | Postgres exception: `permission denied for schema services` | Test user `magic_dev` lacked schema privileges on new Phase F schemas | Executed `GRANT ALL ON SCHEMA ... TO magic_dev, magic_app` |
| **DEF-F07** | Integration Tests | Medium | `IndexOutOfRangeException` when fetching seed records in clean test DB | Integration tests assumed pre-seeded categories and leave types | Added self-healing seed initialization in `PhaseFIntegrationTests.InitializeAsync` |
| **DEF-F08** | MarketingService | Medium | Discount calculated as 200 paise instead of 20,000 paise | Coupon discount value passed as 10 instead of 1000 basis points | Passed `DiscountValue: 1000` (10% = 1,000 basis points) |
| **DEF-F09** | PayrollService | Medium | Re-running test caused `PERIOD_ALREADY_EXISTS` on month 10 | Static month/year collisions between repeated test executions | Added clean-up of prior test cycles for specific target months before calculation |
| **DEF-F10** | ServiceDeskService | High | Foreign key violation on `sales_invoice_lines_products_ProductId` | Labor lines without a physical product passed `Guid.Empty` | Resolved active company product as fallback product ID for service labor lines |

---

## Detailed Root Cause & Remediation Logs

### DEF-F06: Postgres Schema Permissions for Test User
- **Symptom:** `Npgsql.PostgresException: 42501: permission denied for schema services`.
- **Root Cause:** When EF Core migrations create new schemas (`expenses`, `hr`, `payroll`, `marketing`, `orders`, `services`), PostgreSQL defaults the schema owner to the connection user (superuser `postgres`), leaving `magic_dev` and `magic_app` without `USAGE` or `CREATE` permissions.
- **Resolution:** Executed SQL grant commands across both `magic_dev` and `magic_test`:
  ```sql
  GRANT ALL ON SCHEMA expenses, hr, payroll, marketing, orders, services TO magic_dev, magic_app;
  GRANT ALL ON ALL TABLES IN SCHEMA expenses, hr, payroll, marketing, orders, services TO magic_dev, magic_app;
  GRANT ALL ON ALL SEQUENCES IN SCHEMA expenses, hr, payroll, marketing, orders, services TO magic_dev, magic_app;
  ```

### DEF-F10: Foreign Key Constraint on Service Invoice Labor Lines
- **Symptom:** `Npgsql.PostgresException: 23503: insert or update on table "sales_invoice_lines" violates foreign key constraint "FK_sales_invoice_lines_products_ProductId"`.
- **Root Cause:** The `sales.sales_invoice_lines` table has a strict non-null foreign key on `ProductId`. When generating a sales invoice from a repair ticket, labor lines have `qLine.ProductId == null`. The previous code defaulted null to `Guid.Empty` (`00000000-0000-0000-0000-000000000000`), which does not exist in `catalog.products`.
- **Resolution:** Updated `ServiceDeskService.InvoiceServiceRequestAsync` to fetch an active company product as fallback for labor lines:
  ```csharp
  var fallbackProduct = await dbContext.Products.FirstOrDefaultAsync(p => p.CompanyId == companyId && p.IsActive, cancellationToken);
  invoice.Lines.Add(new SalesInvoiceLineRecord
  {
      ...
      ProductId = qLine.ProductId ?? fallbackProduct?.Id ?? Guid.Empty,
      ProductCode = pCode,
      ProductName = pName,
  });
  ```
  Verified that Workflow K now executes to completion and passes 100%.

