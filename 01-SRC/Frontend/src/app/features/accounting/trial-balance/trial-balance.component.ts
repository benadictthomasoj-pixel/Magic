import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TrialBalanceAccountRowDto, TrialBalanceReportDto } from '../../../core/contracts/accounting.models';
import { AccountingApiService } from '../../../core/services/accounting-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'magic-trial-balance',
  standalone: true,
  imports: [CommonModule, FormsModule, StatusBadgeComponent],
  templateUrl: './trial-balance.component.html',
  styleUrl: './trial-balance.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TrialBalanceComponent implements OnInit {
  private readonly api = inject(AccountingApiService);

  protected readonly report = signal<TrialBalanceReportDto | null>(null);
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected asOfDate = new Date().toISOString().split('T')[0];
  protected searchFilter = '';

  ngOnInit(): void {
    this.fetchTrialBalance();
  }

  protected fetchTrialBalance(): void {
    this.loading.set(true);
    this.errorMessage.set(null);

    this.api.getTrialBalance(this.asOfDate).subscribe({
      next: (rep) => {
        this.report.set(rep);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err?.error?.error?.detail || 'Failed to generate trial balance report.');
      },
    });
  }

  protected filteredAccounts(): readonly TrialBalanceAccountRowDto[] {
    const r = this.report();
    if (!r) return [];
    const search = this.searchFilter.toLowerCase().trim();
    if (!search) return r.accounts;

    return r.accounts.filter(
      (a) =>
        a.accountCode.toLowerCase().includes(search) ||
        a.accountName.toLowerCase().includes(search) ||
        a.groupName.toLowerCase().includes(search) ||
        a.rootClass.toLowerCase().includes(search)
    );
  }

  protected formatCurrency(paise: number): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      minimumFractionDigits: 2,
    }).format(paise / 100);
  }
}
