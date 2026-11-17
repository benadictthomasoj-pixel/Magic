using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Magic.Contracts.Api;
using Magic.Contracts.Commercial;
using Magic.Contracts.Finance;
using Magic.Contracts.Security;
using Magic.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Magic.Api.IntegrationTests;

public sealed class FinancialManagementTests : IClassFixture<MagicTestFixture>, IAsyncLifetime
{
    private readonly MagicTestFixture _factory;
    private readonly HttpClient _client;
    private string _adminToken = string.Empty;

    public FinancialManagementTests(MagicTestFixture factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var secSeeder = scope.ServiceProvider.GetRequiredService<SecurityDatabaseSeeder>();
        await secSeeder.SeedAsync();

        var commSeeder = scope.ServiceProvider.GetRequiredService<CommercialDatabaseSeeder>();
        await commSeeder.SeedAsync();

        await AccountingDatabaseSeeder.SeedDevelopmentAccountingAsync(scope.ServiceProvider);

        var adminLogin = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            UsernameOrEmail = "ben",
            Password = "123123"
        });
        var adminAuth = await adminLogin.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
        _adminToken = adminAuth!.Data.AccessToken;
    }

    public Task DisposeAsync() => Task.CompletedTask;

    private HttpClient CreateAdminClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _adminToken);
        return client;
    }

    private static async Task<CustomerSummaryDto> GetOrCreateCustomerAsync(HttpClient client)
    {
        var listRes = await client.GetAsync("/api/v1/commercial/customers?pageSize=10");
        if (listRes.IsSuccessStatusCode)
        {
            var env = await listRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<CustomerSummaryDto>>>();
            if (env?.Data != null && env.Data.Count > 0)
            {
                return env.Data[0];
            }
        }

        var req = new CreateCustomerRequest
        {
            CustomerCode = $"CUST-{Guid.NewGuid().ToString("N")[..4].ToUpperInvariant()}",
            Name = $"Fin Customer {Guid.NewGuid().ToString("N")[..4]}",
            Phone = "9876543210"
        };
        var createRes = await client.PostAsJsonAsync("/api/v1/commercial/customers", req);
        createRes.EnsureSuccessStatusCode();
        var created = await createRes.Content.ReadFromJsonAsync<ApiResponse<CustomerDetailDto>>();
        return new CustomerSummaryDto
        {
            Id = created!.Data.Id,
            CustomerCode = created.Data.CustomerCode,
            Name = created.Data.Name,
            CustomerType = created.Data.CustomerType,
            GstType = created.Data.GstType,
            Phone = created.Data.Phone,
            CreditLimitPaise = 1000000,
            CreditPeriodDays = 30,
            IsCreditHold = false,
            IsActive = true
        };
    }

    private static async Task<SupplierSummaryDto> GetOrCreateSupplierAsync(HttpClient client)
    {
        var req = new CreateSupplierRequest
        {
            SupplierCode = $"SUPP-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            Name = $"Fin Supplier {Guid.NewGuid().ToString("N")[..6]}",
            Category = "Manufacturer",
            Phone = "9876543211",
            PaymentTerms = "Net 30",
            CreditPeriodDays = 30
        };
        var createRes = await client.PostAsJsonAsync("/api/v1/commercial/suppliers", req);
        createRes.EnsureSuccessStatusCode();
        var created = await createRes.Content.ReadFromJsonAsync<ApiResponse<SupplierDetailDto>>();
        return new SupplierSummaryDto
        {
            Id = created!.Data.Id,
            SupplierCode = created.Data.SupplierCode,
            Name = created.Data.Name,
            Category = created.Data.Category,
            GstType = created.Data.GstType,
            Phone = created.Data.Phone,
            PaymentTerms = created.Data.PaymentTerms,
            CreditPeriodDays = created.Data.CreditPeriodDays,
            IsMsmeRegistered = false,
            IsActive = true
        };
    }

    private static async Task<BankCashAccountDto> GetOrCreateBankAccountAsync(HttpClient client)
    {
        var listRes = await client.GetAsync("/api/v1/finance/bank-accounts?accountType=Bank");
        if (listRes.IsSuccessStatusCode)
        {
            var env = await listRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<BankCashAccountDto>>>();
            if (env?.Data != null && env.Data.Count > 0)
            {
                return env.Data[0];
            }
        }

        // Fetch Bank GL account (1112)
        var coaRes = await client.GetAsync("/api/v1/accounting/accounts?rootClass=Asset&search=1112");
        coaRes.EnsureSuccessStatusCode();
        var coaEnv = await coaRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<Magic.Contracts.Accounting.AccountDto>>>();
        var glAcc = coaEnv!.Data.First(a => a.Code == "1112");

        var req = new CreateBankCashAccountRequest
        {
            AccountName = $"HDFC Main Current {Guid.NewGuid().ToString("N")[..4]}",
            AccountType = "Bank",
            BankName = "HDFC Bank",
            BranchName = "Koramangala",
            AccountNumber = "50200012345678",
            IfscCode = "HDFC0000123",
            GlAccountId = glAcc.Id,
            OpeningBalancePaise = 50000000 // 5 Lakhs
        };

        var createRes = await client.PostAsJsonAsync("/api/v1/finance/bank-accounts", req);
        createRes.EnsureSuccessStatusCode();
        var created = await createRes.Content.ReadFromJsonAsync<ApiResponse<BankCashAccountDto>>();
        return created!.Data;
    }

    [Fact]
    public async Task BankAccounts_Create_List_And_Reveal_Masked_Details_With_Audit()
    {
        var client = CreateAdminClient();

        // 1. Fetch GL Account 1111 (Cash)
        var coaRes = await client.GetAsync("/api/v1/accounting/accounts?search=1111");
        var coaEnv = await coaRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<Magic.Contracts.Accounting.AccountDto>>>();
        var cashGl = coaEnv!.Data.First(a => a.Code == "1111");

        // 2. Create Cash Account
        var createReq = new CreateBankCashAccountRequest
        {
            AccountName = $"Main Cash Register {Guid.NewGuid().ToString("N")[..4]}",
            AccountType = "Cash",
            AccountNumber = "CASH-REG-9876",
            GlAccountId = cashGl.Id,
            OpeningBalancePaise = 1000000
        };
        var res = await client.PostAsJsonAsync("/api/v1/finance/bank-accounts", createReq);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var created = await res.Content.ReadFromJsonAsync<ApiResponse<BankCashAccountDto>>();
        Assert.NotNull(created?.Data);
        Assert.Contains('•', created.Data.MaskedAccountNumber);

        // 3. Reveal unmasked account number
        var revealRes = await client.PostAsync($"/api/v1/finance/bank-accounts/{created.Data.Id}/reveal", null);
        Assert.Equal(HttpStatusCode.OK, revealRes.StatusCode);
        var revealed = await revealRes.Content.ReadFromJsonAsync<ApiResponse<RevealAccountNumberResponse>>();
        Assert.Equal("CASH-REG-9876", revealed!.Data.AccountNumber);
    }

    [Fact]
    public async Task SupplierPayables_CreateBill_And_QueryAging()
    {
        var client = CreateAdminClient();
        var supplier = await GetOrCreateSupplierAsync(client);

        var billReq = new CreateSupplierBillRequest
        {
            SupplierId = supplier.Id,
            InvoiceNumber = $"SUP-BILL-{Guid.NewGuid().ToString("N")[..6]}",
            InvoiceDate = new DateOnly(2026, 11, 1),
            DueDate = new DateOnly(2026, 11, 20),
            Notes = "Test inventory raw materials invoice",
            Lines =
            [
                new CreateSupplierBillLineRequest
                {
                    Description = "Steel Fasteners Batch A",
                    Quantity = 100m,
                    UnitPricePaise = 5000, // Rs. 50
                    TaxRatePercentage = 18m
                }
            ]
        };

        var res = await client.PostAsJsonAsync("/api/v1/finance/payables/bills", billReq);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var billEnv = await res.Content.ReadFromJsonAsync<ApiResponse<SupplierBillDto>>();
        Assert.NotNull(billEnv?.Data);
        Assert.Equal(500000L, billEnv.Data.SubTotalPaise); // Rs 5,000
        Assert.Equal(90000L, billEnv.Data.TaxAmountPaise);  // Rs 900
        Assert.Equal(590000L, billEnv.Data.TotalAmountPaise); // Rs 5,900
        Assert.Equal("APPROVED", billEnv.Data.Status);

        // Verify AP aging includes this bill
        var agingRes = await client.GetAsync($"/api/v1/finance/payables/aging?reportDate=2026-11-13");
        Assert.Equal(HttpStatusCode.OK, agingRes.StatusCode);
        var agingEnv = await agingRes.Content.ReadFromJsonAsync<ApiResponse<SupplierAgingReportDto>>();
        Assert.NotNull(agingEnv?.Data);
        Assert.True(agingEnv.Data.TotalOutstandingPaise >= 590000L);
    }

    [Fact]
    public async Task SupplierPayment_Create_And_Settle_Bill_Then_Cancel()
    {
        var client = CreateAdminClient();
        var supplier = await GetOrCreateSupplierAsync(client);
        var bank = await GetOrCreateBankAccountAsync(client);

        // 1. Create a bill for ₹10,000
        var billReq = new CreateSupplierBillRequest
        {
            SupplierId = supplier.Id,
            InvoiceNumber = $"BILL-{Guid.NewGuid().ToString("N")[..6]}",
            InvoiceDate = new DateOnly(2026, 11, 5),
            DueDate = new DateOnly(2026, 11, 25),
            Lines =
            [
                new CreateSupplierBillLineRequest
                {
                    Description = "Component Assembly Parts",
                    Quantity = 1m,
                    UnitPricePaise = 1000000,
                    TaxRatePercentage = 0m
                }
            ]
        };
        var billRes = await client.PostAsJsonAsync("/api/v1/finance/payables/bills", billReq);
        var bill = (await billRes.Content.ReadFromJsonAsync<ApiResponse<SupplierBillDto>>())!.Data;

        // Fetch AP item
        var payablesRes = await client.GetAsync($"/api/v1/finance/payables?supplierId={supplier.Id}");
        var payablesEnv = await payablesRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<SupplierPayableDto>>>();
        var apItem = payablesEnv!.Data.First(p => p.BillNumber == bill.InvoiceNumber);

        // 2. Disburse Payment: ₹10,000 to fully settle
        var payReq = new CreateSupplierPaymentRequest
        {
            SupplierId = supplier.Id,
            PaymentDate = new DateOnly(2026, 11, 10),
            PaymentMethod = "BankTransfer",
            ReferenceNumber = "UTR9988776655",
            BankCashAccountId = bank.Id,
            TotalAmountPaise = 1000000,
            Narration = "Settlement of Component Assembly Parts",
            Allocations =
            [
                new SupplierPaymentAllocationRequest
                {
                    ApItemId = apItem.Id,
                    AllocatedAmountPaise = 1000000
                }
            ]
        };
        var payRes = await client.PostAsJsonAsync("/api/v1/finance/supplier-payments", payReq);
        Assert.Equal(HttpStatusCode.Created, payRes.StatusCode);
        var payment = (await payRes.Content.ReadFromJsonAsync<ApiResponse<SupplierPaymentDto>>())!.Data;
        Assert.Equal(1000000L, payment.AllocatedAmountPaise);
        Assert.Equal(0L, payment.UnallocatedAmountPaise);

        // 3. Reverse payment
        var cancelRes = await client.PostAsJsonAsync($"/api/v1/finance/supplier-payments/{payment.Id}/cancel", new CancelPaymentRequest
        {
            Reason = "Wrong bank account selected for remittance"
        });
        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);
        var reversedPayment = (await cancelRes.Content.ReadFromJsonAsync<ApiResponse<SupplierPaymentDto>>())!.Data;
        Assert.Equal("REVERSED", reversedPayment.Status);
    }

    [Fact]
    public async Task CustomerReceipt_Create_With_Advance_And_Cancel()
    {
        var client = CreateAdminClient();
        var customer = await GetOrCreateCustomerAsync(client);
        var bank = await GetOrCreateBankAccountAsync(client);

        // Create Advance Receipt: ₹25,000 (No allocations, 100% advance)
        var req = new CreateCustomerReceiptRequest
        {
            CustomerId = customer.Id,
            ReceiptDate = new DateOnly(2026, 11, 12),
            PaymentMethod = "UPI",
            ReferenceNumber = "UPI-TXN-11223344",
            BankCashAccountId = bank.Id,
            TotalAmountPaise = 2500000,
            Narration = "Advance token deposit for upcoming project",
            Allocations = []
        };

        var res = await client.PostAsJsonAsync("/api/v1/finance/customer-receipts", req);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        var receipt = (await res.Content.ReadFromJsonAsync<ApiResponse<CustomerReceiptDto>>())!.Data;
        Assert.Equal(2500000L, receipt.TotalAmountPaise);
        Assert.Equal(0L, receipt.AllocatedAmountPaise);
        Assert.Equal(2500000L, receipt.UnallocatedAmountPaise);
        Assert.Equal("SUBMITTED", receipt.Status);

        // Cancel receipt
        var cancelRes = await client.PostAsJsonAsync($"/api/v1/finance/customer-receipts/{receipt.Id}/cancel", new CancelReceiptRequest
        {
            Reason = "Customer requested cancellation of deposit"
        });
        Assert.Equal(HttpStatusCode.OK, cancelRes.StatusCode);
        var reversed = (await cancelRes.Content.ReadFromJsonAsync<ApiResponse<CustomerReceiptDto>>())!.Data;
        Assert.Equal("REVERSED", reversed.Status);
    }

    [Fact]
    public async Task Treasury_ContraTransfer_And_BankReconciliation_Workflow()
    {
        var client = CreateAdminClient();
        var bank = await GetOrCreateBankAccountAsync(client);

        // 1. Fetch Cash GL Account (1111) and create a second Cash account for transfer
        var coaRes = await client.GetAsync("/api/v1/accounting/accounts?search=1111");
        var coaEnv = await coaRes.Content.ReadFromJsonAsync<ApiResponse<IReadOnlyList<Magic.Contracts.Accounting.AccountDto>>>();
        var cashGl = coaEnv!.Data.First(a => a.Code == "1111");

        var cashAccRes = await client.PostAsJsonAsync("/api/v1/finance/bank-accounts", new CreateBankCashAccountRequest
        {
            AccountName = $"Retail Cash Float {Guid.NewGuid().ToString("N")[..4]}",
            AccountType = "Cash",
            AccountNumber = "FLOAT-001",
            GlAccountId = cashGl.Id,
            OpeningBalancePaise = 5000000 // 50,000
        });
        var cashAcc = (await cashAccRes.Content.ReadFromJsonAsync<ApiResponse<BankCashAccountDto>>())!.Data;

        // 2. Perform Contra Cash Deposit into Bank: ₹20,000
        var contraReq = new CreateContraTransactionRequest
        {
            TransactionType = "CashDeposit",
            TransactionDate = new DateOnly(2026, 11, 12),
            SourceAccountId = cashAcc.Id,
            DestinationAccountId = bank.Id,
            AmountPaise = 2000000,
            ReferenceNumber = "DEP-CHQ-101",
            Narration = "Daily store cash deposit to bank"
        };
        var contraRes = await client.PostAsJsonAsync("/api/v1/finance/treasury/contra", contraReq);
        Assert.Equal(HttpStatusCode.OK, contraRes.StatusCode);
        var tx = (await contraRes.Content.ReadFromJsonAsync<ApiResponse<TreasuryTransactionDto>>())!.Data;
        Assert.Equal("POSTED", tx.Status);

        // 3. Bank Reconciliation Import CSV
        var csvData = "Date,Description,Reference,Withdrawal,Deposit,Balance\n" +
                      "2026-11-12,Daily store cash deposit,DEP-CHQ-101,0,20000,520000\n" +
                      "2026-11-12,Monthly Account Service Fee,CHG-998,250,0,519750\n";

        var importReq = new ImportBankStatementRequest
        {
            BankAccountId = bank.Id,
            StatementStartDate = new DateOnly(2026, 11, 1),
            StatementEndDate = new DateOnly(2026, 11, 30),
            OpeningBalancePaise = 50000000,
            ClosingBalancePaise = 51975000,
            Filename = "HDFC_Nov_2026.csv",
            CsvContent = csvData
        };
        var importRes = await client.PostAsJsonAsync("/api/v1/finance/bank-reconciliation/import-csv", importReq);
        Assert.Equal(HttpStatusCode.OK, importRes.StatusCode);
        var stmtDetail = (await importRes.Content.ReadFromJsonAsync<ApiResponse<BankStatementDetailDto>>())!.Data;
        Assert.Equal(2, stmtDetail.Lines.Count);

        // 4. Fetch Workspace
        var wsRes = await client.GetAsync($"/api/v1/finance/bank-reconciliation/workspace/{stmtDetail.Id}");
        Assert.Equal(HttpStatusCode.OK, wsRes.StatusCode);
        var ws = (await wsRes.Content.ReadFromJsonAsync<ApiResponse<ReconciliationWorkspaceDto>>())!.Data;
        Assert.Equal(2, ws.StatementLines.Count);

        // 5. Match deposit line with journal line
        if (ws.UnmatchedLedgerLines.Count > 0)
        {
            var matchReq = new MatchStatementLineRequest
            {
                StatementLineId = ws.StatementLines[0].Id,
                JournalLineId = ws.UnmatchedLedgerLines[0].JournalLineId,
                MatchType = "MANUAL"
            };
            var matchRes = await client.PostAsJsonAsync($"/api/v1/finance/bank-reconciliation/match?statementId={stmtDetail.Id}", matchReq);
            Assert.Equal(HttpStatusCode.OK, matchRes.StatusCode);
        }

        // 6. Complete Reconciliation
        var compRes = await client.PostAsync($"/api/v1/finance/bank-reconciliation/complete/{stmtDetail.Id}", null);
        Assert.Equal(HttpStatusCode.OK, compRes.StatusCode);
        var compSummary = (await compRes.Content.ReadFromJsonAsync<ApiResponse<BankStatementSummaryDto>>())!.Data;
        Assert.Equal("RECONCILED", compSummary.Status);
    }

    [Fact]
    public async Task FinancialReports_CashBook_BankBook_And_SummaryKPIs()
    {
        var client = CreateAdminClient();

        // 1. High-level KPIs
        var kpiRes = await client.GetAsync("/api/v1/finance/reports/summary");
        Assert.Equal(HttpStatusCode.OK, kpiRes.StatusCode);
        var kpis = await kpiRes.Content.ReadFromJsonAsync<ApiResponse<FinanceSummaryKpiDto>>();
        Assert.NotNull(kpis?.Data);

        // 2. Cash Book
        var cashBookRes = await client.GetAsync("/api/v1/finance/reports/cash-book?fromDate=2026-04-01&toDate=2026-11-30");
        Assert.Equal(HttpStatusCode.OK, cashBookRes.StatusCode);
        var cashBook = await cashBookRes.Content.ReadFromJsonAsync<ApiResponse<CashBookReportDto>>();
        Assert.NotNull(cashBook?.Data);

        // 3. Bank Book
        var bankBookRes = await client.GetAsync("/api/v1/finance/reports/bank-book?fromDate=2026-04-01&toDate=2026-11-30");
        Assert.Equal(HttpStatusCode.OK, bankBookRes.StatusCode);
        var bankBook = await bankBookRes.Content.ReadFromJsonAsync<ApiResponse<BankBookReportDto>>();
        Assert.NotNull(bankBook?.Data);
    }
}
