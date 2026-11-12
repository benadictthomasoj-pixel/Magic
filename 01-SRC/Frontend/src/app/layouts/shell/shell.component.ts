import { A11yModule } from '@angular/cdk/a11y';
import { BreakpointObserver } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthBoundaryService } from '../../core/services/auth-boundary.service';
import { ConnectivityMonitorService } from '../../core/services/connectivity-monitor.service';
import { ThemePreference, ThemeService } from '../../core/services/theme.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

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

  // Enterprise Organizational Context
  protected readonly companyName = signal('Universal Enterprises Ltd');
  protected readonly branchName = signal('Headquarters (MAIN)');
  protected readonly fiscalYear = signal('FY 2026-27');

  protected readonly navSections: readonly NavSection[] = [
    {
      title: 'Financial Accounting',
      items: [
        { path: '/accounting/dashboard', label: 'Overview / KPI', icon: '📊' },
        { path: '/accounting/chart-of-accounts', label: 'Chart of Accounts', icon: '📑' },
        { path: '/accounting/journal-entry', label: 'Journal Entry', icon: '✍️' },
        { path: '/accounting/vouchers', label: 'Voucher Registry', icon: '📋' },
        { path: '/accounting/general-ledger', label: 'General Ledger', icon: '📖' },
        { path: '/accounting/trial-balance', label: 'Trial Balance', icon: '⚖️' },
        { path: '/accounting/periods', label: 'Accounting Periods', icon: '📅' },
        { path: '/accounting/subledgers', label: 'AR / AP Subledgers', icon: '👥' },
      ],
    },
    {
      title: 'Upcoming Modules',
      items: [
        { path: '', label: 'Financial Statements', icon: '📈', badge: 'Soon', disabled: true },
        { path: '', label: 'Sales & Invoicing', icon: '🛒', badge: 'Phase 3', disabled: true },
        { path: '', label: 'Purchases & AP', icon: '📦', badge: 'Phase 3', disabled: true },
        { path: '', label: 'Point of Sale (POS)', icon: '💳', badge: 'Soon', disabled: true },
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
  }

  ngOnInit(): void {
    this.monitor.startPolling();
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
