import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  CustomerAgingReportDto,
  CustomerReceivableDto,
  CustomerStatementDto,
  ReceivablesSummaryDto,
} from '../../../core/contracts/finance.models';
import { FinanceApiService } from '../../../core/services/finance-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'magic-receivables-management',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, StatusBadgeComponent],
  templateUrl: './receivables-management.component.html',
  styleUrl: './receivables-management.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReceivablesManagementComponent implements OnInit {
  private readonly api = inject(FinanceApiService);

  protected readonly activeTab = signal<'invoices' | 'aging'>('invoices');
  protected readonly summary = signal<ReceivablesSummaryDto | null>(null);
  protected readonly receivables = signal<readonly CustomerReceivableDto[]>([]);
  protected readonly agingReport = signal<CustomerAgingReportDto | null>(null);
  protected readonly selectedStatement = signal<CustomerStatementDto | null>(null);
  protected readonly loading = signal(true);

  protected filterStatus = '';
  protected filterOverdueOnly = false;
  protected agingReportDate = new Date().toISOString().split('T')[0];

  ngOnInit(): void {
    this.loadSummary();
    this.loadReceivables();
  }

  protected setTab(tab: 'invoices' | 'aging'): void {
    this.activeTab.set(tab);
    if (tab === 'aging' && !this.agingReport()) {
      this.loadAging();
    }
  }

  protected loadSummary(): void {
    this.api.getReceivablesSummary().subscribe({
      next: (res) => this.summary.set(res),
      error: () => {},
    });
  }

  protected loadReceivables(): void {
    this.loading.set(true);
    this.api.getReceivables({
      status: this.filterStatus || undefined,
      overdueOnly: this.filterOverdueOnly || undefined,
    }).subscribe({
      next: (res) => {
        this.receivables.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadAging(): void {
    this.loading.set(true);
    this.api.getCustomerAging(this.agingReportDate).subscribe({
      next: (res) => {
        this.agingReport.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected viewStatement(customerId: string): void {
    this.api.getCustomerStatement(customerId).subscribe({
      next: (res) => this.selectedStatement.set(res),
      error: () => {},
    });
  }

  protected closeStatement(): void {
    this.selectedStatement.set(null);
  }

  protected formatPaise(paise: number): string {
    return (paise / 100).toLocaleString('en-IN', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
  }
}
