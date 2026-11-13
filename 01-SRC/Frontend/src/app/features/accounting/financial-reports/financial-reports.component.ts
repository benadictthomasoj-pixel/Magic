import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  BankBookReportDto,
  BankCashAccountDto,
  CashBookReportDto,
  FinanceSummaryKpiDto,
} from '../../../core/contracts/finance.models';
import { FinanceApiService } from '../../../core/services/finance-api.service';

@Component({
  selector: 'magic-financial-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './financial-reports.component.html',
  styleUrl: './financial-reports.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FinancialReportsComponent implements OnInit {
  private readonly financeApi = inject(FinanceApiService);

  protected readonly activeTab = signal<'kpi' | 'cashbook' | 'bankbook'>('kpi');
  protected readonly summaryKpi = signal<FinanceSummaryKpiDto | null>(null);
  protected readonly cashBook = signal<CashBookReportDto | null>(null);
  protected readonly bankBook = signal<BankBookReportDto | null>(null);
  protected readonly bankAccounts = signal<readonly BankCashAccountDto[]>([]);
  protected readonly cashAccounts = signal<readonly BankCashAccountDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly actionMessage = signal<{ type: 'ok' | 'error'; text: string } | null>(null);

  protected selectedBankAccountId = '';
  protected selectedCashAccountId = '';
  protected fromDate = new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().slice(0, 10);
  protected toDate = new Date().toISOString().slice(0, 10);

  ngOnInit(): void {
    this.loadAccounts();
    this.loadSummary();
  }

  protected setTab(tab: 'kpi' | 'cashbook' | 'bankbook'): void {
    this.activeTab.set(tab);
    if (tab === 'kpi') {
      this.loadSummary();
    } else if (tab === 'cashbook') {
      this.loadCashBook();
    } else if (tab === 'bankbook') {
      this.loadBankBook();
    }
  }

  private loadAccounts(): void {
    this.financeApi.getBankAccounts(undefined, true).subscribe({
      next: (accounts) => {
        const banks = accounts.filter((a) => a.accountType !== 'Cash');
        const cashes = accounts.filter((a) => a.accountType === 'Cash');
        this.bankAccounts.set(banks);
        this.cashAccounts.set(cashes);

        if (banks.length > 0) this.selectedBankAccountId = banks[0].id;
        if (cashes.length > 0) this.selectedCashAccountId = cashes[0].id;
      },
      error: () => {},
    });
  }

  protected loadSummary(): void {
    this.loading.set(true);
    this.financeApi.getFinancialSummary().subscribe({
      next: (data) => {
        this.summaryKpi.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.actionMessage.set({
          type: 'error',
          text: err?.error?.error?.detail || 'Failed to load financial summary KPIs.',
        });
        this.loading.set(false);
      },
    });
  }

  protected loadCashBook(): void {
    this.loading.set(true);
    this.financeApi
      .getCashBook({
        accountId: this.selectedCashAccountId || undefined,
        fromDate: this.fromDate,
        toDate: this.toDate,
      })
      .subscribe({
        next: (data) => {
          this.cashBook.set(data);
          this.loading.set(false);
        },
        error: (err) => {
          this.actionMessage.set({
            type: 'error',
            text: err?.error?.error?.detail || 'Failed to load Cash Book report.',
          });
          this.loading.set(false);
        },
      });
  }

  protected loadBankBook(): void {
    if (!this.selectedBankAccountId) return;
    this.loading.set(true);
    this.financeApi
      .getBankBook({
        bankAccountId: this.selectedBankAccountId,
        fromDate: this.fromDate,
        toDate: this.toDate,
      })
      .subscribe({
        next: (data) => {
          this.bankBook.set(data);
          this.loading.set(false);
        },
        error: (err) => {
          this.actionMessage.set({
            type: 'error',
            text: err?.error?.error?.detail || 'Failed to load Bank Book report.',
          });
          this.loading.set(false);
        },
      });
  }

  protected formatPaise(paise: number): string {
    const rupees = (paise / 100).toLocaleString('en-IN', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
    return `₹${rupees}`;
  }
}
