import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../contracts/api.models';
import { BankCashAccountDto, CustomerReceivableDto, ReceivablesSummaryDto } from '../contracts/finance.models';
import { FinanceApiService } from './finance-api.service';

describe('FinanceApiService', () => {
  let service: FinanceApiService;
  let httpMock: HttpTestingController;
  const baseUrl = environment.apiBaseUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        FinanceApiService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(FinanceApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch receivables summary', () => {
    const mockSummary: ReceivablesSummaryDto = {
      totalReceivablePaise: 1500000,
      totalOverduePaise: 500000,
      customerCount: 12,
      openInvoiceCount: 18,
    };

    const mockResponse: ApiResponse<ReceivablesSummaryDto> = {
      success: true,
      data: mockSummary,
      correlationId: 'test-123',
      timestampUtc: '2026-11-13T10:00:00Z',
    };

    service.getReceivablesSummary().subscribe((summary) => {
      expect(summary.totalReceivablePaise).toBe(1500000);
      expect(summary.openInvoiceCount).toBe(18);
    });

    const req = httpMock.expectOne(`${baseUrl}/v1/accounting/receivables/summary`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should fetch bank and cash accounts', () => {
    const mockAccounts: BankCashAccountDto[] = [
      {
        id: 'acc-1',
        accountName: 'HDFC Corporate Current',
        accountType: 'Bank',
        maskedAccountNumber: '••••••••1234',
        glAccountId: 'gl-1',
        glAccountCode: '1112',
        glAccountName: 'Bank Account',
        openingBalancePaise: 5000000,
        currentBalancePaise: 5500000,
        isActive: true,
        createdAtUtc: '2026-11-13T10:00:00Z',
      },
    ];

    const mockResponse: ApiResponse<BankCashAccountDto[]> = {
      success: true,
      data: mockAccounts,
      correlationId: 'test-456',
      timestampUtc: '2026-11-13T10:00:00Z',
    };

    service.getBankAccounts('Bank').subscribe((accounts) => {
      expect(accounts.length).toBe(1);
      expect(accounts[0].accountName).toBe('HDFC Corporate Current');
    });

    const req = httpMock.expectOne(`${baseUrl}/v1/accounting/bank-accounts?accountType=Bank`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });
});
