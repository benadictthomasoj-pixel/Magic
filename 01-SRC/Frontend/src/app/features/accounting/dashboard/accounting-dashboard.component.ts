import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AccountingDashboardDto } from '../../../core/contracts/accounting.models';
import { AccountingApiService } from '../../../core/services/accounting-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'magic-accounting-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink, StatusBadgeComponent],
  templateUrl: './accounting-dashboard.component.html',
  styleUrl: './accounting-dashboard.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountingDashboardComponent implements OnInit {
  private readonly api = inject(AccountingApiService);

  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly data = signal<AccountingDashboardDto | null>(null);

  ngOnInit(): void {
    this.loadDashboard();
  }

  protected loadDashboard(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.getDashboard().subscribe({
      next: (dto) => {
        this.data.set(dto);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.error?.detail || 'Unable to connect to accounting service.');
        this.loading.set(false);
      },
    });
  }

  protected formatPaise(paise: number): string {
    const rupees = paise / 100;
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      minimumFractionDigits: 2,
    }).format(rupees);
  }
}
