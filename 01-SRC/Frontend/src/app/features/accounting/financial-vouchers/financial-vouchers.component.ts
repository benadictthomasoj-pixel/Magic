import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { VoucherDetailDto, VoucherSummaryDto } from '../../../core/contracts/accounting.models';
import { AccountingApiService } from '../../../core/services/accounting-api.service';

@Component({
  selector: 'magic-financial-vouchers',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './financial-vouchers.component.html',
  styleUrl: './financial-vouchers.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FinancialVouchersComponent implements OnInit {
  private readonly accountingApi = inject(AccountingApiService);

  protected readonly vouchers = signal<readonly VoucherSummaryDto[]>([]);
  protected readonly selectedVoucher = signal<VoucherDetailDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly isReversing = signal(false);
  protected readonly actionMessage = signal<{ type: 'ok' | 'error'; text: string } | null>(null);

  protected filterType = '';
  protected filterStatus = '';
  protected filterSearch = '';
  protected filterFromDate = '';
  protected filterToDate = '';

  ngOnInit(): void {
    this.loadVouchers();
  }

  protected loadVouchers(): void {
    this.loading.set(true);
    this.accountingApi
      .getVouchers({
        voucherType: this.filterType || undefined,
        status: this.filterStatus || undefined,
        search: this.filterSearch || undefined,
        dateFrom: this.filterFromDate || undefined,
        dateTo: this.filterToDate || undefined,
      })
      .subscribe({
        next: (res) => {
          this.vouchers.set(res.items);
          this.loading.set(false);
        },
        error: (err) => {
          this.actionMessage.set({
            type: 'error',
            text: err?.error?.error?.detail || 'Failed to load financial vouchers.',
          });
          this.loading.set(false);
        },
      });
  }

  protected selectVoucher(v: VoucherSummaryDto): void {
    this.accountingApi.getVoucherById(v.id).subscribe({
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

    const reason = prompt(
      `Enter reason for non-destructive reversal of voucher ${v.voucherNumber || v.id}:`,
      'Correction of erroneous commercial / financial entry'
    );
    if (!reason) return;

    this.isReversing.set(true);
    this.actionMessage.set(null);

    this.accountingApi.reverseVoucher(v.id, { branchCode: 'MAIN', fyCode: 'FY26', reason }).subscribe({
      next: (reversed) => {
        this.isReversing.set(false);
        this.actionMessage.set({
          type: 'ok',
          text: `Voucher successfully reversed. Non-destructive compensating voucher created: ${reversed.voucherNumber}`,
        });
        this.selectedVoucher.set(null);
        this.loadVouchers();
      },
      error: (err) => {
        this.isReversing.set(false);
        this.actionMessage.set({
          type: 'error',
          text: err?.error?.error?.detail || 'Reversal failed.',
        });
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
