import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { SubledgerReportDto } from '../../../core/contracts/accounting.models';
import { AccountingApiService } from '../../../core/services/accounting-api.service';

@Component({
  selector: 'magic-subledgers',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './subledgers.component.html',
  styleUrl: './subledgers.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SubledgersComponent implements OnInit {
  private readonly api = inject(AccountingApiService);

  protected readonly activeTab = signal<'AR' | 'AP'>('AR');
  protected readonly arSummary = signal<SubledgerReportDto | null>(null);
  protected readonly apSummary = signal<SubledgerReportDto | null>(null);
  protected readonly loading = signal(true);

  ngOnInit(): void {
    this.loadSubledgers();
  }

  protected loadSubledgers(): void {
    this.loading.set(true);
    this.api.getSubledgerReport('ar').subscribe({
      next: (ar) => {
        this.arSummary.set(ar);
        this.api.getSubledgerReport('ap').subscribe({
          next: (ap) => {
            this.apSummary.set(ap);
            this.loading.set(false);
          },
          error: () => this.loading.set(false),
        });
      },
      error: () => this.loading.set(false),
    });
  }

  protected setTab(tab: 'AR' | 'AP'): void {
    this.activeTab.set(tab);
  }

  protected currentSummary(): SubledgerReportDto | null {
    return this.activeTab() === 'AR' ? this.arSummary() : this.apSummary();
  }

  protected formatCurrency(paise: number): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      minimumFractionDigits: 2,
    }).format(paise / 100);
  }
}
