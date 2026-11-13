import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  BankCashAccountDto,
  SupplierPaymentDto,
} from '../../../core/contracts/finance.models';
import { SupplierSummaryDto } from '../../../core/contracts/commercial.models';
import { CommercialApiService } from '../../../core/services/commercial-api.service';
import { FinanceApiService } from '../../../core/services/finance-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

interface ApAllocationFormRow {
  apItemId: string;
  billNumber: string;
  dueDate: string;
  outstandingPaise: number;
  allocatedPaise: number;
  isSelected: boolean;
}

@Component({
  selector: 'magic-supplier-payments',
  standalone: true,
  imports: [CommonModule, FormsModule, StatusBadgeComponent],
  templateUrl: './supplier-payments.component.html',
  styleUrl: './supplier-payments.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SupplierPaymentsComponent implements OnInit {
  private readonly api = inject(FinanceApiService);
  private readonly commercialApi = inject(CommercialApiService);

  protected readonly payments = signal<readonly SupplierPaymentDto[]>([]);
  protected readonly selectedPayment = signal<SupplierPaymentDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly isCreating = signal(false);
  protected readonly isCancelling = signal(false);

  // New Payment Modal State
  protected showCreateModal = false;
  protected suppliers = signal<readonly SupplierSummaryDto[]>([]);
  protected bankAccounts = signal<readonly BankCashAccountDto[]>([]);
  protected openApItems = signal<ApAllocationFormRow[]>([]);

  protected formSupplierId = '';
  protected formPaymentDate = new Date().toISOString().split('T')[0];
  protected formPaymentMethod = 'BankTransfer';
  protected formReferenceNumber = '';
  protected formBankAccountId = '';
  protected formTotalAmount = 0; // in Rupees
  protected formNarration = '';
  protected cancelReason = '';

  ngOnInit(): void {
    this.loadPayments();
    this.loadPrerequisites();
  }

  protected loadPayments(): void {
    this.loading.set(true);
    this.api.getPayments().subscribe({
      next: (res) => {
        this.payments.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadPrerequisites(): void {
    this.commercialApi.getSuppliers().subscribe({
      next: (res) => this.suppliers.set(res.items),
      error: () => {},
    });

    this.api.getBankAccounts(undefined, true).subscribe({
      next: (accounts) => {
        this.bankAccounts.set(accounts);
        if (accounts.length > 0) {
          this.formBankAccountId = accounts[0].id;
        }
      },
      error: () => {},
    });
  }

  protected onSupplierSelected(): void {
    if (!this.formSupplierId) {
      this.openApItems.set([]);
      return;
    }

    this.api.getPayables({ supplierId: this.formSupplierId, status: 'OPEN' }).subscribe({
      next: (res) => {
        const rows: ApAllocationFormRow[] = res.items.map((i) => ({
          apItemId: i.id,
          billNumber: i.billNumber,
          dueDate: i.dueDate,
          outstandingPaise: i.outstandingAmountPaise,
          allocatedPaise: 0,
          isSelected: false,
        }));
        this.openApItems.set(rows);
      },
      error: () => {},
    });
  }

  protected autoAllocate(): void {
    let remainingPaise = Math.round(this.formTotalAmount * 100);
    const updated = this.openApItems().map((item) => {
      if (remainingPaise <= 0) {
        return { ...item, allocatedPaise: 0, isSelected: false };
      }
      const canAlloc = Math.min(remainingPaise, item.outstandingPaise);
      remainingPaise -= canAlloc;
      return {
        ...item,
        allocatedPaise: canAlloc,
        isSelected: canAlloc > 0,
      };
    });
    this.openApItems.set(updated);
  }

  protected calculateAllocatedPaise(): number {
    return this.openApItems()
      .filter((i) => i.isSelected)
      .reduce((sum, i) => sum + i.allocatedPaise, 0);
  }

  protected calculateUnallocatedPaise(): number {
    const totalPaise = Math.round(this.formTotalAmount * 100);
    return Math.max(0, totalPaise - this.calculateAllocatedPaise());
  }

  protected submitPayment(): void {
    if (!this.formSupplierId || !this.formBankAccountId || this.formTotalAmount <= 0) {
      return;
    }

    this.isCreating.set(true);
    const totalPaise = Math.round(this.formTotalAmount * 100);
    const allocations = this.openApItems()
      .filter((i) => i.isSelected && i.allocatedPaise > 0)
      .map((i) => ({
        apItemId: i.apItemId,
        allocatedAmountPaise: i.allocatedPaise,
      }));

    this.api.createPayment({
      supplierId: this.formSupplierId,
      paymentDate: this.formPaymentDate,
      paymentMethod: this.formPaymentMethod,
      referenceNumber: this.formReferenceNumber || undefined,
      bankCashAccountId: this.formBankAccountId,
      totalAmountPaise: totalPaise,
      narration: this.formNarration || undefined,
      allocations,
    }).subscribe({
      next: (created) => {
        this.isCreating.set(false);
        this.showCreateModal = false;
        this.loadPayments();
        this.selectedPayment.set(created);
      },
      error: () => this.isCreating.set(false),
    });
  }

  protected confirmCancelPayment(): void {
    const p = this.selectedPayment();
    if (!p || !this.cancelReason) return;

    this.isCancelling.set(true);
    this.api.cancelPayment(p.id, { reason: this.cancelReason }).subscribe({
      next: (reversed) => {
        this.isCancelling.set(false);
        this.selectedPayment.set(reversed);
        this.loadPayments();
      },
      error: () => this.isCancelling.set(false),
    });
  }

  protected printPaymentAdvice(): void {
    window.print();
  }

  protected formatPaise(paise: number): string {
    return (paise / 100).toLocaleString('en-IN', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
  }
}
