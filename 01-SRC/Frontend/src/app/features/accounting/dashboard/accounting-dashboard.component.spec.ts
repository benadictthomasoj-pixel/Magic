import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { AccountingDashboardComponent } from './accounting-dashboard.component';
import { AccountingApiService } from '../../../core/services/accounting-api.service';
import { AccountingDashboardDto } from '../../../core/contracts/accounting.models';

describe('AccountingDashboardComponent', () => {
  let mockApiService: Partial<AccountingApiService>;

  const mockMetrics: AccountingDashboardDto = {
    totalDebitPaise: 25000000,
    totalCreditPaise: 25000000,
    postedVouchersCount: 14,
    draftVouchersCount: 2,
    activeFiscalYearName: 'FY 2026-27',
    activeFiscalPeriodCode: 'M01-APR',
    isActivePeriodLocked: false,
    recentVouchers: [],
    systemValidationStatus: 'BALANCED',
  };

  beforeEach(async () => {
    mockApiService = {
      getDashboard: vi.fn().mockReturnValue(of(mockMetrics)),
    };

    await TestBed.configureTestingModule({
      imports: [AccountingDashboardComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: AccountingApiService, useValue: mockApiService },
      ],
    }).compileComponents();
  });

  it('should render accounting overview title and loaded metrics', () => {
    const fixture = TestBed.createComponent(AccountingDashboardComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.page-title')?.textContent).toContain('Financial Accounting Overview');
    expect(compiled.querySelector('.metric-value')?.textContent).toBeDefined();
  });
});
