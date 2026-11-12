import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { VoucherDetailDto, VoucherSummaryDto } from '../../../core/contracts/accounting.models';
import { AccountingApiService } from '../../../core/services/accounting-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'magic-vouchers-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, StatusBadgeComponent],
  templateUrl: './vouchers-list.component.html',
  styleUrl: './vouchers-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VouchersListComponent implements OnInit {
  private readonly api = inject(AccountingApiService);

  protected readonly vouchers = signal<readonly VoucherSummaryDto[]>([]);
  protected readonly selectedVoucher = signal<VoucherDetailDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly isReversing = signal(false);
  protected readonly actionMessage = signal<{ type: 'ok' | 'error'; text: string } | null>(null);

  protected filterStatus = '';
  protected filterSearch = '';

  ngOnInit(): void {
    this.loadVouchers();
  }

  protected loadVouchers(): void {
    this.loading.set(true);
    this.api.getVouchers({
      status: this.filterStatus || undefined,
      search: this.filterSearch || undefined,
    }).subscribe({
      next: (res) => {
        this.vouchers.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected selectVoucher(v: VoucherSummaryDto): void {
    this.api.getVoucherById(v.id).subscribe({
      next: (detail) => this.selectedVoucher.set(detail),
      error: () => {},
    });
  }

  protected closeDrawer(): void {
    this.selectedVoucher.set(null);
  }

  protected reverseSelectedVoucher(): void {
    const v = this.selectedVoucher();
    if (!v) return;

    const reason = prompt(`Enter reason for reversing voucher ${v.voucherNumber || v.id}:`, 'Correction of incorrect transaction');
    if (!reason) return;

    this.isReversing.set(true);
    this.actionMessage.set(null);

    this.api.reverseVoucher(v.id, { branchCode: 'MAIN', fyCode: 'FY26', reason }).subscribe({
      next: (reversed) => {
        this.isReversing.set(false);
        this.actionMessage.set({
          type: 'ok',
          text: `Voucher successfully reversed. Compensating voucher created: ${reversed.voucherNumber}`,
        });
        this.selectedVoucher.set(null);
        this.loadVouchers();
      },
      error: (err) => {
        this.isReversing.set(false);
        this.actionMessage.set({
          type: 'error',
          text: err?.error?.error?.detail || 'Reversal failed: Authenticated user identity required (OPEN-003).',
        });
      },
    });
  }

  protected filteredVouchers(): readonly VoucherSummaryDto[] {
    const search = this.filterSearch.toLowerCase().trim();
    return this.vouchers().filter((v) => {
      const matchSearch =
        !search ||
        (v.voucherNumber && v.voucherNumber.toLowerCase().includes(search)) ||
        v.narration.toLowerCase().includes(search) ||
        (v.referenceNumber && v.referenceNumber.toLowerCase().includes(search));
      return matchSearch;
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
