import { A11yModule } from '@angular/cdk/a11y';
import { BreakpointObserver } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthBoundaryService } from '../../core/services/auth-boundary.service';
import { ConnectivityMonitorService } from '../../core/services/connectivity-monitor.service';
import { ThemePreference, ThemeService } from '../../core/services/theme.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

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

  protected readonly isNarrowScreen = signal(false);
  protected readonly mobileNavOpen = signal(false);

  protected readonly navItems = [
    { path: '/system/status', label: 'Platform Status' },
    { path: '/system/preferences', label: 'Display & Context' },
  ] as const;

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
}

