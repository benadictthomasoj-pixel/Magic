import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { AccountingApiService } from './accounting-api.service';
import { AccountingDashboardDto } from '../contracts/accounting.models';

describe('AccountingApiService', () => {
  let service: AccountingApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        AccountingApiService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(AccountingApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should fetch accounting dashboard metrics', () => {
    const mockDashboard: AccountingDashboardDto = {
      totalDebitPaise: 100000,
      totalCreditPaise: 100000,
      postedVouchersCount: 5,
      draftVouchersCount: 1,
      activeFiscalYearName: 'FY 2026-27',
      activeFiscalPeriodCode: 'M01-APR',
      isActivePeriodLocked: false,
      recentVouchers: [],
      systemValidationStatus: 'BALANCED',
    };

    service.getDashboard().subscribe((data) => {
      expect(data.systemValidationStatus).toBe('BALANCED');
      expect(data.postedVouchersCount).toBe(5);
    });

    const req = httpMock.expectOne('http://127.0.0.1:5000/api/v1/accounting/dashboard');
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: mockDashboard });
  });

  it('should fetch chart of accounts list', () => {
    service.getAccounts().subscribe((data) => {
      expect(data.length).toBe(1);
      expect(data[0].code).toBe('11110');
    });

    const req = httpMock.expectOne('http://127.0.0.1:5000/api/v1/accounting/accounts');
    expect(req.request.method).toBe('GET');
    req.flush({
      success: true,
      data: [
        {
          id: 'acc-1',
          groupId: 'grp-1',
          groupName: 'Cash and Cash Equivalents',
          code: '11110',
          name: 'Main Cash Account',
          rootClass: 'Asset',
          currencyCode: 'INR',
          isActive: true,
          currentBalancePaise: 50000,
        },
      ],
    });
  });

  it('should call validate journal voucher endpoint', () => {
    service
      .validateJournal({
        fiscalPeriodId: 'fp-1',
        voucherType: 'Journal',
        voucherDate: '2026-04-15',
        narration: 'Test entry',
        lines: [],
      })
      .subscribe((res) => {
        expect(res.isValid).toBe(true);
      });

    const req = httpMock.expectOne('http://127.0.0.1:5000/api/v1/accounting/vouchers/validate');
    expect(req.request.method).toBe('POST');
    req.flush({
      success: true,
      data: {
        isValid: true,
        totalDebitPaise: 10000,
        totalCreditPaise: 10000,
        differencePaise: 0,
        isBalanced: true,
        errors: [],
        warnings: [],
      },
    });
  });
});
