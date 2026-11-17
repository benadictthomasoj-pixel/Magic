import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { CashManagementComponent } from './cash-management.component';
import { SalesApiService } from '../../../core/services/sales-api.service';
import { CashierSessionDto } from '../../../core/contracts/sales.models';

describe('CashManagementComponent', () => {
  let mockSalesApi: Partial<SalesApiService>;

  const mockOpenSession: CashierSessionDto = {
    id: '398396d2-3b60-4d6a-8197-55aaae180975',
    branchId: '22222222-2222-2222-2222-222222222222',
    terminalId: null,
    cashierUserId: '7247a184-6804-4dc4-af1b-f4e30b919e19',
    cashierUsername: 'Benadict (Administrator)',
    openedAtUtc: '2026-11-17T09:01:31.12528Z',
    closedAtUtc: null,
    status: 'Open',
    openingCashPaise: 500000,
    cashSalesPaise: 120000,
    cashRefundsPaise: 0,
    cashDepositsPaise: 0,
    cashWithdrawalsPaise: 0,
    expectedCashPaise: 620000,
    actualClosingCashPaise: null,
    variancePaise: null,
    closingNotes: null,
    supervisorUserId: null,
    approvedAtUtc: null,
    movements: [
      {
        id: 'mv-1',
        cashierSessionId: '398396d2-3b60-4d6a-8197-55aaae180975',
        movementType: 'FloatAdjustment',
        amountPaise: 500000,
        reason: 'Initial Opening Cash Float',
        movementDateUtc: '2026-11-17T09:01:31.12528Z',
        cashierUserId: '7247a184-6804-4dc4-af1b-f4e30b919e19',
      },
    ],
  };

  const mockSessionsHistory = {
    items: [mockOpenSession],
    totalCount: 1,
    page: 1,
    pageSize: 30,
  };

  beforeEach(async () => {
    mockSalesApi = {
      getActiveSession: vi.fn().mockReturnValue(of(null)),
      getSessions: vi.fn().mockReturnValue(of(mockSessionsHistory)),
      openSession: vi.fn().mockReturnValue(of(mockOpenSession)),
      recordMovement: vi.fn().mockReturnValue(of({} as any)),
      closeSession: vi.fn().mockReturnValue(of({ ...mockOpenSession, status: 'Closed', variancePaise: 0 })),
    };

    await TestBed.configureTestingModule({
      imports: [CashManagementComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: SalesApiService, useValue: mockSalesApi },
      ],
    }).compileComponents();
  });

  it('renders closed shift banner when no active shift exists', () => {
    const fixture = TestBed.createComponent(CashManagementComponent);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('No Active Cashier Shift');
    expect(el.querySelector('.shift-closed-banner')).toBeTruthy();
  });

  it('renders active shift card when active session is open', () => {
    (mockSalesApi.getActiveSession as any).mockReturnValue(of(mockOpenSession));
    const fixture = TestBed.createComponent(CashManagementComponent);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Active Shift: Benadict (Administrator)');
    expect(el.textContent).toContain('Expected Drawer Cash');
    expect(el.querySelector('.shift-closed-banner')).toBeNull();
  });

  it('opens shift instantly and updates activeSession signal without getting stuck', () => {
    (mockSalesApi.getActiveSession as any).mockReturnValue(of(mockOpenSession));
    const fixture = TestBed.createComponent(CashManagementComponent);
    fixture.detectChanges();

    const comp = fixture.componentInstance;
    comp.openOpenModal();
    fixture.detectChanges();
    expect(comp.showOpenModal()).toBe(true);

    comp.openingFloatRupees = 5000;
    comp.openShift();
    fixture.detectChanges();

    expect(mockSalesApi.openSession).toHaveBeenCalledWith({ openingCashPaise: 500000 });
    expect(comp.isOpening()).toBe(false);
    expect(comp.showOpenModal()).toBe(false);
    expect(comp.activeSession()).toEqual(mockOpenSession);
    expect(comp.successMessage()).toContain('Cashier shift opened successfully with ₹5000.00 float!');
  });

  it('handles openShift error gracefully and resets isOpening flag', () => {
    (mockSalesApi.openSession as any).mockReturnValue(
      throwError(() => ({
        error: {
          error: {
            detail: 'You already have an active cashier shift open. Please close it before opening a new shift.',
          },
        },
      }))
    );

    const fixture = TestBed.createComponent(CashManagementComponent);
    fixture.detectChanges();

    const comp = fixture.componentInstance;
    comp.openOpenModal();
    fixture.detectChanges();

    comp.openShift();
    fixture.detectChanges();

    expect(comp.isOpening()).toBe(false);
    expect(comp.errorMessage()).toContain('You already have an active cashier shift open');
  });
});
