import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { catchError, forkJoin, of } from 'rxjs';
import { SalesByProductDto, SalesInvoiceDto } from '../../core/contracts/sales.models';
import { AuthBoundaryService } from '../../core/services/auth-boundary.service';
import { CommercialApiService } from '../../core/services/commercial-api.service';
import { Branch, EnterpriseApiService } from '../../core/services/enterprise-api.service';
import { InventoryApiService } from '../../core/services/inventory-api.service';
import { SalesApiService } from '../../core/services/sales-api.service';
import { formatPaise } from '../../core/utils/money.util';

export interface TrendPoint {
  readonly label: string;
  readonly shortLabel: string;
  readonly amountPaise: number;
  readonly x: number;
  readonly y: number;
}

/**
 * Desktop Billing Dashboard Component for MAGIC ERP.
 *
 * Implements the approved simple, clean, professional desktop visual reference:
 * - 4 Compact KPI Cards in one horizontal row (Total Sales, Number of Bills, Active Customers, Stock Value).
 * - Row 1 (50% / 50%): Period-aware Sales Trend line chart & 5 practical Quick Actions.
 * - Row 2 (40% / 60%): Top Selling Products & Recent Transactions with View All.
 * - Branch selector, date-period selector, and refresh button.
 * - ZERO stock alerts or decorative notification panels.
 */
@Component({
  selector: 'magic-billing-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './billing-dashboard.component.html',
  styleUrl: './billing-dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BillingDashboardComponent implements OnInit {
  private readonly salesApi = inject(SalesApiService);
  private readonly commercialApi = inject(CommercialApiService);
  private readonly inventoryApi = inject(InventoryApiService);
  private readonly enterpriseApi = inject(EnterpriseApiService);
  private readonly auth = inject(AuthBoundaryService);

  // Loading & Error States
  protected readonly loading = signal<boolean>(true);
  protected readonly refreshing = signal<boolean>(false);
  protected readonly error = signal<string | null>(null);

  // Filters
  protected readonly selectedBranchId = signal<string>('');
  protected readonly selectedPeriod = signal<string>('this-month');
  protected readonly branches = signal<readonly Branch[]>([]);

  // KPI Metrics (Strict 64-bit integer paise where currency)
  protected readonly totalSalesPaise = signal<number>(0);
  protected readonly totalBillsCount = signal<number>(0);
  protected readonly activeCustomersCount = signal<number>(0);
  protected readonly stockValuePaise = signal<number>(0);

  // Trend, Products & Transactions Data
  protected readonly trendRawData = signal<Array<{ label: string; shortLabel: string; amountPaise: number }>>([]);
  protected readonly topProducts = signal<readonly SalesByProductDto[]>([]);
  protected readonly recentTransactions = signal<readonly SalesInvoiceDto[]>([]);

  // Period Display Label
  protected readonly periodLabel = computed(() => {
    switch (this.selectedPeriod()) {
      case 'today':
        return 'Today';
      case 'this-week':
        return 'This Week';
      case 'this-month':
        return 'This Month';
      case 'this-quarter':
        return 'This Quarter';
      case 'this-year':
        return 'This Fiscal Year';
      default:
        return 'Selected Period';
    }
  });

  // Chart Coordinates Calculations
  protected readonly chartMaxPaise = computed(() => {
    const raw = this.trendRawData();
    const amounts = raw.map((d) => d.amountPaise);
    const maxVal = amounts.length > 0 ? Math.max(...amounts) : 0;
    return maxVal > 0 ? maxVal : 1000000; // Default ₹10,000 scale when no sales
  });

  protected readonly chartMaxLabel = computed(() => {
    return this.formatCompactCurrency(this.chartMaxPaise());
  });

  protected readonly chartMidLabel = computed(() => {
    return this.formatCompactCurrency(Math.round(this.chartMaxPaise() / 2));
  });

  protected readonly isTrendZero = computed(() => {
    const raw = this.trendRawData();
    return raw.length === 0 || raw.every((d) => d.amountPaise === 0);
  });

  protected readonly chartPoints = computed<TrendPoint[]>(() => {
    const raw = this.trendRawData();
    if (raw.length === 0) return [];

    const max = this.chartMaxPaise();
    const leftX = 65;
    const rightX = 520;
    const topY = 25;
    const baseY = 185;
    const availableWidth = rightX - leftX;
    const availableHeight = baseY - topY;

    const count = raw.length;
    const step = count > 1 ? availableWidth / (count - 1) : availableWidth;

    return raw.map((item, idx) => {
      const x = count === 1 ? leftX + availableWidth / 2 : leftX + idx * step;
      const ratio = Math.min(1, Math.max(0, item.amountPaise / max));
      const y = baseY - ratio * availableHeight;
      return {
        label: item.label,
        shortLabel: item.shortLabel,
        amountPaise: item.amountPaise,
        x: Math.round(x * 10) / 10,
        y: Math.round(y * 10) / 10,
      };
    });
  });

  protected readonly chartLinePath = computed(() => {
    const points = this.chartPoints();
    if (points.length === 0) return '';
    return points.reduce((acc, pt, idx) => {
      return idx === 0 ? `M ${pt.x} ${pt.y}` : `${acc} L ${pt.x} ${pt.y}`;
    }, '');
  });

  protected readonly chartAreaPath = computed(() => {
    const points = this.chartPoints();
    if (points.length === 0) return '';
    const linePath = this.chartLinePath();
    const firstX = points[0].x;
    const lastX = points[points.length - 1].x;
    return `${linePath} L ${lastX} 185 L ${firstX} 185 Z`;
  });

  ngOnInit(): void {
    // Set initial branch from user context if available
    const activeBranch = this.auth.branch();
    if (activeBranch?.id) {
      this.selectedBranchId.set(activeBranch.id);
    }
    this.loadDashboardData();
  }

  protected onBranchChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    this.selectedBranchId.set(target.value);
    this.loadDashboardData();
  }

  protected onPeriodChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    this.selectedPeriod.set(target.value);
    this.loadDashboardData();
  }

  protected refreshDashboard(): void {
    this.refreshing.set(true);
    this.loadDashboardData(() => {
      this.refreshing.set(false);
    });
  }

  protected loadDashboardData(onComplete?: () => void): void {
    this.error.set(null);
    const branchId = this.selectedBranchId() || undefined;
    const { fromDate, toDate } = this.getPeriodDateRange(this.selectedPeriod());

    forkJoin({
      branches: this.enterpriseApi.getBranches().pipe(catchError(() => of([]))),
      summary: this.salesApi.getSummary({ branchId, fromDate, toDate }).pipe(
        catchError((err) => {
          this.error.set(err?.message || 'Failed to load sales metrics from LAN server.');
          return of(null);
        })
      ),
      customers: this.commercialApi.getCustomers({ isActive: true, pageSize: 1 }).pipe(catchError(() => of({ totalCount: 0, items: [] }))),
      stock: this.inventoryApi.getStockBalances({ pageSize: 100 }).pipe(catchError(() => of({ items: [], totalCount: 0 }))),
      topProds: this.salesApi.getTopProducts({ branchId, fromDate, toDate, limit: 5 }).pipe(catchError(() => of([]))),
      invoices: this.salesApi.getInvoices({ branchId, fromDate, toDate, pageSize: 5 }).pipe(catchError(() => of({ items: [], totalCount: 0 }))),
      hourly: this.selectedPeriod() === 'today' ? this.salesApi.getHourlySales({ branchId, date: toDate }).pipe(catchError(() => of([]))) : of(null),
      salesReport: this.selectedPeriod() !== 'today' ? this.enterpriseApi.getReport({ reportType: 'sales-by-period', fromDate, toDate, branchId }).pipe(catchError(() => of(null))) : of(null),
    }).subscribe({
      next: (results) => {
        // 1. Branches
        if (results.branches.length > 0) {
          this.branches.set(results.branches);
        }

        // 2. Sales Summary
        if (results.summary) {
          this.totalSalesPaise.set(results.summary.netSalesPaise || results.summary.grossSalesPaise || 0);
          this.totalBillsCount.set(results.summary.totalInvoicesCount || 0);
        } else {
          this.totalSalesPaise.set(0);
          this.totalBillsCount.set(0);
        }

        // 3. Active Customers Count
        this.activeCustomersCount.set(results.customers.totalCount || results.customers.items?.length || 0);

        // 4. Stock Valuation Calculation
        let calculatedStockPaise = 0;
        if (results.stock.items && results.stock.items.length > 0) {
          for (const item of results.stock.items) {
            const cost = item.averageCostPaise || 0;
            calculatedStockPaise += Math.round(item.quantityOnHand * cost);
          }
        }
        this.stockValuePaise.set(calculatedStockPaise);

        // 5. Top Products
        this.topProducts.set(results.topProds || []);

        // 6. Recent Invoices
        this.recentTransactions.set(results.invoices.items || []);

        // 7. Sales Trend Points Construction
        this.constructTrendData(results.hourly, results.salesReport, fromDate, toDate);

        this.loading.set(false);
        if (onComplete) onComplete();
      },
      error: (err) => {
        this.error.set(err?.message || 'Failed to load billing dashboard data.');
        this.loading.set(false);
        if (onComplete) onComplete();
      },
    });
  }

  private constructTrendData(
    hourly: readonly any[] | null,
    salesReport: any | null,
    fromDate: string,
    toDate: string
  ): void {
    if (this.selectedPeriod() === 'today') {
      if (hourly && hourly.length > 0) {
        // Group into 6 4-hour buckets for compact rendering
        const buckets: Array<{ label: string; shortLabel: string; amountPaise: number }> = [
          { label: '00:00 - 04:00', shortLabel: '4 AM', amountPaise: 0 },
          { label: '04:00 - 08:00', shortLabel: '8 AM', amountPaise: 0 },
          { label: '08:00 - 12:00', shortLabel: '12 PM', amountPaise: 0 },
          { label: '12:00 - 16:00', shortLabel: '4 PM', amountPaise: 0 },
          { label: '16:00 - 20:00', shortLabel: '8 PM', amountPaise: 0 },
          { label: '20:00 - 24:00', shortLabel: '12 AM', amountPaise: 0 },
        ];
        for (const h of hourly) {
          const bIdx = Math.min(5, Math.floor(h.hourOfDay / 4));
          buckets[bIdx].amountPaise += h.totalSalesPaise || 0;
        }
        this.trendRawData.set(buckets);
        return;
      }
    }

    // If report returned period rows
    if (salesReport?.rows && salesReport.rows.length > 0) {
      const rows = salesReport.rows.map((r: any) => ({
        label: r.period || r.date || 'Period',
        shortLabel: (r.period || r.date || '').slice(-5),
        amountPaise: r.amountPaise || r.totalPaise || 0,
      }));
      this.trendRawData.set(rows);
      return;
    }

    // Default Baseline: 7 clean intervals representing the selected window
    const intervals = this.generateBaselineIntervals(fromDate, toDate);
    this.trendRawData.set(intervals);
  }

  private generateBaselineIntervals(fromDate: string, toDate: string): Array<{ label: string; shortLabel: string; amountPaise: number }> {
    const start = new Date(fromDate);
    const end = new Date(toDate);
    const points: Array<{ label: string; shortLabel: string; amountPaise: number }> = [];

    const numPoints = 6;
    const diff = (end.getTime() - start.getTime()) / (numPoints - 1);

    for (let i = 0; i < numPoints; i++) {
      const d = new Date(start.getTime() + i * diff);
      const day = d.toLocaleDateString([], { month: 'short', day: 'numeric' });
      points.push({
        label: day,
        shortLabel: day,
        amountPaise: 0,
      });
    }
    return points;
  }

  private getPeriodDateRange(period: string): { fromDate: string; toDate: string } {
    const now = new Date();
    const toDate = now.toISOString().split('T')[0];
    let from = new Date(now);

    switch (period) {
      case 'today':
        from = new Date(now.getFullYear(), now.getMonth(), now.getDate());
        break;
      case 'this-week': {
        const day = now.getDay();
        const diff = now.getDate() - day + (day === 0 ? -6 : 1);
        from = new Date(now.setDate(diff));
        break;
      }
      case 'this-month':
        from = new Date(now.getFullYear(), now.getMonth(), 1);
        break;
      case 'this-quarter': {
        const qMonth = Math.floor(now.getMonth() / 3) * 3;
        from = new Date(now.getFullYear(), qMonth, 1);
        break;
      }
      case 'this-year':
        from = new Date(now.getFullYear(), 0, 1);
        break;
    }
    const fromDate = from.toISOString().split('T')[0];
    return { fromDate, toDate };
  }

  protected formatPaise(paise: number | null | undefined): string {
    return formatPaise(paise ?? 0);
  }

  protected formatCompactCurrency(paise: number): string {
    const rupees = Math.round(paise / 100);
    if (rupees >= 10000000) {
      return `₹${(rupees / 10000000).toFixed(1)}Cr`;
    }
    if (rupees >= 100000) {
      return `₹${(rupees / 100000).toFixed(1)}L`;
    }
    if (rupees >= 1000) {
      return `₹${(rupees / 1000).toFixed(0)}k`;
    }
    return `₹${rupees}`;
  }

  protected formatTxnTime(isoDate: string | null | undefined): string {
    if (!isoDate) return '—';
    try {
      const d = new Date(isoDate);
      return d.toLocaleDateString([], { month: 'short', day: 'numeric' }) + ' ' + d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    } catch {
      return isoDate;
    }
  }
}
