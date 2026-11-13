import { A11yModule } from '@angular/cdk/a11y';
import { BreakpointObserver } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { AuthBoundaryService } from '../../core/services/auth-boundary.service';
import { ConnectivityMonitorService } from '../../core/services/connectivity-monitor.service';
import { ThemePreference, ThemeService } from '../../core/services/theme.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

export type AppModule = 'accounting' | 'billing';

export interface NavItem {
  readonly path: string;
  readonly label: string;
  readonly icon?: string;
  readonly badge?: string;
  readonly disabled?: boolean;
}

export interface NavSection {
  readonly title: string;
  readonly items: readonly NavItem[];
}

@Component({
  selector: 'magic-shell',
  standalone: true,
  imports: [A11yModule, RouterOutlet, RouterLink, RouterLinkActive, StatusBadgeComponent],
  templateUrl: './shell.component.html',
  styleUrl: './shell.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(document:keydown.escape)': 'closeMobileNav()',
  },
})
export class ShellComponent implements OnInit {
  private readonly breakpointObserver = inject(BreakpointObserver);
  protected readonly monitor = inject(ConnectivityMonitorService);
  protected readonly theme = inject(ThemeService);
  protected readonly auth = inject(AuthBoundaryService);
  private readonly router = inject(Router);

  protected readonly isNarrowScreen = signal(false);
  protected readonly mobileNavOpen = signal(false);

  // Active Business Module: 'billing' | 'accounting'
  protected readonly currentModule = signal<AppModule>('accounting');

  // Enterprise Organizational Context
  protected readonly companyName = signal('Universal Enterprises Ltd');
  protected readonly branchName = signal('Headquarters (MAIN)');
  protected readonly fiscalYear = signal('FY 2026-27');

  // Accounting Module Navigation Sections
  private readonly accountingSections: readonly NavSection[] = [
    {
      title: 'Financial Accounting',
      items: [
        { path: '/accounting/dashboard', label: 'Accounting Overview', icon: '📊' },
        { path: '/accounting/chart-of-accounts', label: 'Chart of Accounts', icon: '📑' },
        { path: '/accounting/journal-entry', label: 'Journal Entry', icon: '✍️' },
        { path: '/accounting/vouchers', label: 'Voucher Registry', icon: '📋' },
        { path: '/accounting/general-ledger', label: 'General Ledger', icon: '📖' },
        { path: '/accounting/trial-balance', label: 'Trial Balance', icon: '⚖️' },
        { path: '/accounting/periods', label: 'Fiscal Years & Periods', icon: '📅' },
        { path: '/accounting/subledgers', label: 'AR / AP Subledgers', icon: '👥' },
      ],
    },
    {
      title: 'System & Governance',
      items: [
        { path: '/system/status', label: 'Platform Status', icon: '🛡️' },
        { path: '/system/preferences', label: 'Display & Context', icon: '⚙️' },
      ],
    },
  ];

  // Billing Module Navigation Sections (15 Planned Entries)
  private readonly billingSections: readonly NavSection[] = [
    {
      title: 'POS & Desk Operations',
      items: [
        { path: '/billing/dashboard', label: 'Billing Dashboard', icon: '📊' },
        { path: '/billing/pos', label: 'POS / New Bill', icon: '💳' },
        { path: '/billing/cash-closing', label: 'Cash Closing', icon: '🔒' },
      ],
    },
    {
      title: 'Sales & Parties',
      items: [
        { path: '/billing/sales', label: 'Sales', icon: '🧾' },
        { path: '/billing/sales-returns', label: 'Sales Returns', icon: '↩️' },
        { path: '/billing/customers', label: 'Customers', icon: '👥' },
        { path: '/billing/customer-payments', label: 'Customer Payments', icon: '💵' },
      ],
    },
    {
      title: 'Catalog & Master Data',
      items: [
        { path: '/billing/products', label: 'Products', icon: '🏷️' },
        { path: '/billing/categories', label: 'Categories', icon: '📁' },
        { path: '/billing/customers', label: 'Customers', icon: '👥' },
        { path: '/billing/suppliers', label: 'Suppliers', icon: '🏢' },
      ],
    },
    {
      title: 'Inventory Management',
      items: [
        { path: '/billing/warehouses', label: 'Warehouses', icon: '🏛️' },
        { path: '/billing/inventory', label: 'Stock Status & Ledger', icon: '📦' },
        { path: '/billing/stock-adjustments', label: 'Stock Adjustments', icon: '🔄' },
        { path: '/billing/stock-counts', label: 'Stock Counting', icon: '📝' },
        { path: '/billing/stock-transfers', label: 'Warehouse Transfers', icon: '🚚' },
        { path: '/billing/reorder-management', label: 'Reorder Alerts', icon: '🔔' },
      ],
    },
    {
      title: 'Procurement & Inbound',
      items: [
        { path: '/billing/purchase-orders', label: 'Purchase Orders', icon: '🛒' },
        { path: '/billing/goods-receipts', label: 'Goods Receipts (GRN)', icon: '📥' },
        { path: '/billing/purchase-returns', label: 'Purchase Returns', icon: '↩️' },
        { path: '/billing/expenses', label: 'Expenses', icon: '📉' },
      ],
    },
    {
      title: 'Reports & Settings',
      items: [
        { path: '/billing/reports', label: 'Billing Reports', icon: '📈' },
        { path: '/billing/settings', label: 'Billing Settings', icon: '⚙️' },
      ],
    },
    {
      title: 'System & Governance',
      items: [
        { path: '/system/status', label: 'Platform Status', icon: '🛡️' },
        { path: '/system/preferences', label: 'Display & Context', icon: '🎨' },
      ],
    },
  ];

  // Dynamically resolve active sidebar navigation based on selected module
  protected readonly navSections = computed<readonly NavSection[]>(() => {
    return this.currentModule() === 'billing' ? this.billingSections : this.accountingSections;
  });

  constructor() {
    this.breakpointObserver
      .observe('(max-width: 47.999rem)')
      .pipe(takeUntilDestroyed())
      .subscribe((result) => {
        this.isNarrowScreen.set(result.matches);
        if (!result.matches) {
          this.mobileNavOpen.set(false);
        }
      });

    // Detect module from initial route
    this.syncModuleWithUrl(this.router.url);

    // Track route navigation changes to keep module switcher in exact sync
    this.router.events
      .pipe(
        filter((event): event is NavigationEnd => event instanceof NavigationEnd),
        takeUntilDestroyed()
      )
      .subscribe((event) => {
        this.syncModuleWithUrl(event.urlAfterRedirects || event.url);
      });
  }

  ngOnInit(): void {
    this.monitor.startPolling();
  }

  private syncModuleWithUrl(url: string): void {
    if (url.startsWith('/billing')) {
      this.currentModule.set('billing');
    } else if (url.startsWith('/accounting')) {
      this.currentModule.set('accounting');
    }
  }

  protected switchModule(targetModule: AppModule): void {
    if (this.currentModule() === targetModule) return;

    this.currentModule.set(targetModule);
    this.closeMobileNav();

    if (targetModule === 'billing') {
      this.router.navigate(['/billing/dashboard']);
    } else {
      this.router.navigate(['/accounting/dashboard']);
    }
  }

  protected toggleMobileNav(): void {
    this.mobileNavOpen.update((open) => !open);
  }

  protected closeMobileNav(): void {
    if (this.mobileNavOpen()) {
      this.mobileNavOpen.set(false);
    }
  }

  protected cycleTheme(): void {
    const order: readonly ThemePreference[] = ['system', 'light', 'dark'];
    const currentIndex = order.indexOf(this.theme.preference());
    const next = order[(currentIndex + 1) % order.length];
    this.theme.setPreference(next);
  }

  protected signOut(): void {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
