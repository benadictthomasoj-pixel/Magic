import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AccountDto, GeneralLedgerReportDto } from '../../../core/contracts/accounting.models';
import { AccountingApiService } from '../../../core/services/accounting-api.service';

@Component({
  selector: 'magic-general-ledger',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './general-ledger.component.html',
  styleUrl: './general-ledger.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GeneralLedgerComponent implements OnInit {
  private readonly api = inject(AccountingApiService);

  protected readonly accounts = signal<readonly AccountDto[]>([]);
  protected readonly report = signal<GeneralLedgerReportDto | null>(null);
  protected readonly loading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected selectedAccountId = '';
  protected fromDate = new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().split('T')[0];
  protected toDate = new Date().toISOString().split('T')[0];

  ngOnInit(): void {
    this.loadAccounts();
  }

  private loadAccounts(): void {
    this.api.getAccounts(undefined, true).subscribe({
      next: (accs) => {
        this.accounts.set(accs);
        if (accs.length > 0) {
          this.selectedAccountId = accs[0].id;
          this.fetchLedger();
        }
      },
      error: () => {},
    });
  }

  protected fetchLedger(): void {
    if (!this.selectedAccountId) return;

    this.loading.set(true);
    this.errorMessage.set(null);

    this.api.getGeneralLedger(this.selectedAccountId, this.fromDate, this.toDate).subscribe({
      next: (rep) => {
        this.report.set(rep);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.errorMessage.set(err?.error?.error?.detail || 'Failed to load general ledger statement.');
      },
    });
  }

  protected formatCurrency(paise: number): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      minimumFractionDigits: 2,
    }).format(paise / 100);
  }
}
