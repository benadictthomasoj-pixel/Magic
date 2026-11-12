import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FiscalYearDto } from '../../../core/contracts/accounting.models';
import { AccountingApiService } from '../../../core/services/accounting-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'magic-accounting-periods',
  standalone: true,
  imports: [CommonModule, StatusBadgeComponent],
  templateUrl: './accounting-periods.component.html',
  styleUrl: './accounting-periods.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountingPeriodsComponent implements OnInit {
  private readonly api = inject(AccountingApiService);

  protected readonly fiscalYears = signal<readonly FiscalYearDto[]>([]);
  protected readonly selectedYear = signal<FiscalYearDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly actionMessage = signal<{ type: 'ok' | 'error'; text: string } | null>(null);

  ngOnInit(): void {
    this.loadYears();
  }

  protected loadYears(): void {
    this.loading.set(true);
    this.api.getFiscalYears().subscribe({
      next: (fys) => {
        this.fiscalYears.set(fys);
        if (fys.length > 0 && !this.selectedYear()) {
          this.selectedYear.set(fys[0]);
        } else if (this.selectedYear()) {
          const updated = fys.find((y) => y.id === this.selectedYear()!.id);
          if (updated) this.selectedYear.set(updated);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected selectYear(fy: FiscalYearDto): void {
    this.selectedYear.set(fy);
  }

  protected lockPeriod(periodId: string, code: string): void {
    if (!confirm(`Are you sure you want to LOCK period ${code}? No new vouchers can be posted in a locked period.`)) return;

    this.actionMessage.set(null);
    this.api.lockFiscalPeriod(periodId).subscribe({
      next: () => {
        this.actionMessage.set({ type: 'ok', text: `Period ${code} has been successfully LOCKED.` });
        this.loadYears();
      },
      error: (err) => {
        this.actionMessage.set({
          type: 'error',
          text: err?.error?.error?.detail || 'Lock operation rejected by governance boundary (OPEN-003).',
        });
      },
    });
  }

  protected closePeriod(periodId: string, code: string): void {
    if (!confirm(`Move period ${code} to CLOSING status?`)) return;

    this.actionMessage.set(null);
    this.api.closeFiscalPeriod(periodId).subscribe({
      next: () => {
        this.actionMessage.set({ type: 'ok', text: `Period ${code} moved to CLOSING status.` });
        this.loadYears();
      },
      error: (err) => {
        this.actionMessage.set({
          type: 'error',
          text: err?.error?.error?.detail || 'Close operation rejected by governance boundary (OPEN-003).',
        });
      },
    });
  }

  protected reopenPeriod(periodId: string, code: string): void {
    if (!confirm(`REOPEN locked period ${code}? This permits retroactive postings.`)) return;

    this.actionMessage.set(null);
    this.api.reopenFiscalPeriod(periodId).subscribe({
      next: () => {
        this.actionMessage.set({ type: 'ok', text: `Period ${code} has been REOPENED.` });
        this.loadYears();
      },
      error: (err) => {
        this.actionMessage.set({
          type: 'error',
          text: err?.error?.error?.detail || 'Reopen operation rejected by governance boundary (OPEN-003).',
        });
      },
    });
  }
}
