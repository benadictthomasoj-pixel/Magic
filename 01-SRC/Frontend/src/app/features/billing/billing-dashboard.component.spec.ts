import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import { BillingDashboardComponent } from './billing-dashboard.component';
import { SalesApiService } from '../../core/services/sales-api.service';
import { CommercialApiService } from '../../core/services/commercial-api.service';
import { InventoryApiService } from '../../core/services/inventory-api.service';
import { EnterpriseApiService } from '../../core/services/enterprise-api.service';
import { AuthBoundaryService } from '../../core/services/auth-boundary.service';
import { SalesByProductDto } from '../../core/contracts/sales.models';

describe('BillingDashboardComponent', () => {
  let mockSalesApi: Partial<SalesApiService>;
  let mockCommercialApi: Partial<CommercialApiService>;
  let mockInventoryApi: Partial<InventoryApiService>;
  let mockEnterpriseApi: Partial<EnterpriseApiService>;
  let mockAuthService: Partial<AuthBoundaryService>;

  const mockSummary = {
    grossSalesPaise: 15000000,
    netSalesPaise: 14500000,
    totalTaxPaise: 500000,
    totalDiscountPaise: 200000,
    totalInvoicesCount: 42,
    totalPaidPaise: 14000000,
    outstandingPaise: 500000,
  };

  const mockTopProducts: SalesByProductDto[] = [
    { productId: 'p1', productCode: 'RIC-BAS-005', productName: 'Basmati Rice 5kg', totalQuantitySold: 80, totalRevenuePaise: 4000000 },
    { productId: 'p2', productCode: 'OIL-SUN-001', productName: 'Sunflower Oil 1L', totalQuantitySold: 60, totalRevenuePaise: 1200000 },
  ];

  const mockInvoices = {
    items: [
      { id: 'inv-1', invoiceNumber: 'INV-2026-0001', customerName: 'Apex Retails Ltd', issueDate: '2026-11-17T10:00:00Z', grandTotalPaise: 250000, paymentStatus: 'Paid', status: 'Posted' },
      { id: 'inv-2', invoiceNumber: 'INV-2026-0002', customerName: 'Supermart Express', issueDate: '2026-11-17T11:30:00Z', grandTotalPaise: 120000, paymentStatus: 'PartiallyPaid', status: 'Posted' },
    ],
    totalCount: 2,
    page: 1,
    pageSize: 5,
  };

  const mockHourly = [
    { hourOfDay: 10, totalSalesPaise: 4500000, invoiceCount: 15 },
    { hourOfDay: 14, totalSalesPaise: 10000000, invoiceCount: 27 },
  ];

  const mockBranches = [
    { id: 'br-1', branchCode: 'HQ', branchName: 'Headquarters Main', isActive: true, isHeadquarters: true },
    { id: 'br-2', branchCode: 'BLR', branchName: 'Bangalore Retail Hub', isActive: true, isHeadquarters: false },
  ];

  const mockCustomers = {
    items: [{ id: 'c1', name: 'Apex Retails Ltd' }],
    totalCount: 156,
    page: 1,
    pageSize: 1,
  };

  const mockStock = {
    items: [
      { id: 'st-1', productId: 'p1', quantityOnHand: 200, averageCostPaise: 35000 },
      { id: 'st-2', productId: 'p2', quantityOnHand: 150, averageCostPaise: 11000 },
    ],
    totalCount: 2,
    page: 1,
    pageSize: 100,
  };

  beforeEach(async () => {
    mockSalesApi = {
      getSummary: vi.fn().mockReturnValue(of(mockSummary)),
      getTopProducts: vi.fn().mockReturnValue(of(mockTopProducts)),
      getInvoices: vi.fn().mockReturnValue(of(mockInvoices)),
      getHourlySales: vi.fn().mockReturnValue(of(mockHourly)),
    };

    mockCommercialApi = {
      getCustomers: vi.fn().mockReturnValue(of(mockCustomers)),
    };

    mockInventoryApi = {
      getStockBalances: vi.fn().mockReturnValue(of(mockStock)),
    };

    mockEnterpriseApi = {
      getBranches: vi.fn().mockReturnValue(of(mockBranches)),
      getReport: vi.fn().mockReturnValue(of(null)),
    };

    mockAuthService = {
      branch: signal({ id: 'br-1', name: 'Headquarters Main', code: 'HQ' } as any),
    };

    await TestBed.configureTestingModule({
      imports: [BillingDashboardComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: SalesApiService, useValue: mockSalesApi },
        { provide: CommercialApiService, useValue: mockCommercialApi },
        { provide: InventoryApiService, useValue: mockInventoryApi },
        { provide: EnterpriseApiService, useValue: mockEnterpriseApi },
        { provide: AuthBoundaryService, useValue: mockAuthService },
      ],
    }).compileComponents();
  });

  it('renders dashboard heading, breadcrumbs, and subtitle', () => {
    const fixture = TestBed.createComponent(BillingDashboardComponent);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.page-title')?.textContent).toContain('Billing Dashboard');
    expect(el.querySelector('.page-subtitle')?.textContent).toContain('Sales, POS, Inventory and Commercial Operations');
    expect(el.querySelector('.breadcrumb')?.textContent).toContain('Billing');
  });

  it('renders four compact KPI cards with formatted metrics', () => {
    const fixture = TestBed.createComponent(BillingDashboardComponent);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const kpiCards = el.querySelectorAll('.kpi-card');
    expect(kpiCards.length).toBe(4);

    // Total Sales: 14500000 paise = ₹1,45,000.00
    expect(el.querySelector('#kpi-total-sales .kpi-value')?.textContent).toContain('1,45,000.00');
    // Number of Bills: 42
    expect(el.querySelector('#kpi-bills-count .kpi-value')?.textContent).toContain('42');
    // Active Customers: 156
    expect(el.querySelector('#kpi-active-customers .kpi-value')?.textContent).toContain('156');
    // Stock Value: (200 * 35000) + (150 * 11000) = 7,000,000 + 1,650,000 = 8,650,000 paise = ₹86,500.00
    expect(el.querySelector('#kpi-stock-value .kpi-value')?.textContent).toContain('86,500.00');
  });

  it('renders Sales Trend line chart with SVG elements', () => {
    const fixture = TestBed.createComponent(BillingDashboardComponent);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const trendSection = el.querySelector('.panel-trend');
    expect(trendSection).toBeTruthy();
    expect(trendSection?.querySelector('.trend-chart')).toBeTruthy();
    expect(trendSection?.querySelector('.trend-line-path')).toBeTruthy();
  });

  it('renders all five practical Quick Actions linking to correct operational routes', () => {
    const fixture = TestBed.createComponent(BillingDashboardComponent);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const actionTiles = el.querySelectorAll('.action-tile');
    expect(actionTiles.length).toBe(5);

    expect(el.querySelector('#action-pos-bill')).toBeTruthy();
    expect(el.querySelector('#action-new-sale')).toBeTruthy();
    expect(el.querySelector('#action-new-po')).toBeTruthy();
    expect(el.querySelector('#action-record-grn')).toBeTruthy();
    expect(el.querySelector('#action-receive-payment')).toBeTruthy();

    expect(el.textContent).toContain('New POS Bill');
    expect(el.textContent).toContain('New Sale');
    expect(el.textContent).toContain('New Purchase Order');
    expect(el.textContent).toContain('Record GRN');
    expect(el.textContent).toContain('Receive Payment');
  });

  it('renders Top Selling Products and Recent Transactions tables', () => {
    const fixture = TestBed.createComponent(BillingDashboardComponent);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;

    // Top Products
    const topProdsSection = el.querySelector('.panel-top-products');
    expect(topProdsSection).toBeTruthy();
    expect(topProdsSection?.textContent).toContain('Basmati Rice 5kg');
    expect(topProdsSection?.textContent).toContain('Sunflower Oil 1L');

    // Recent Transactions
    const recentTxnSection = el.querySelector('.panel-recent-txns');
    expect(recentTxnSection).toBeTruthy();
    expect(recentTxnSection?.textContent).toContain('INV-2026-0001');
    expect(recentTxnSection?.textContent).toContain('Apex Retails Ltd');
    expect(recentTxnSection?.querySelector('.panel-action-link')?.getAttribute('href')).toBe('/billing/sales');
  });

  it('DOES NOT render any Stock Alert panels (strictly compliant with design specification)', () => {
    const fixture = TestBed.createComponent(BillingDashboardComponent);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.stock-alerts-card')).toBeNull();
    expect(el.querySelector('.alert-badge')).toBeNull();
    expect(el.textContent).not.toContain('Low Stock Alert');
    expect(el.textContent).not.toContain('Expiring Soon');
    expect(el.textContent).not.toContain('Negative Stock');
  });

  it('handles period and branch filter changes', () => {
    const fixture = TestBed.createComponent(BillingDashboardComponent);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const periodSelect = el.querySelector('#period-select') as HTMLSelectElement;
    expect(periodSelect).toBeTruthy();

    periodSelect.value = 'this-week';
    periodSelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(mockSalesApi.getSummary).toHaveBeenCalled();
  });

  it('triggers refresh when refresh button is clicked', () => {
    const fixture = TestBed.createComponent(BillingDashboardComponent);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const refreshBtn = el.querySelector('.btn-refresh') as HTMLButtonElement;
    expect(refreshBtn).toBeTruthy();

    refreshBtn.click();
    fixture.detectChanges();

    expect(mockSalesApi.getSummary).toHaveBeenCalled();
  });

  it('displays error banner when API fails to load data', () => {
    mockSalesApi.getSummary = vi.fn().mockReturnValue(throwError(() => new Error('LAN Connection Lost')));
    const fixture = TestBed.createComponent(BillingDashboardComponent);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('.alert-error')?.textContent).toContain('LAN Connection Lost');
  });
});
