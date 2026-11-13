import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  BankCashAccountDto,
  CustomerReceiptDto,
  CustomerReceivableDto,
} from '../../../core/contracts/finance.models';
import { CustomerSummaryDto } from '../../../core/contracts/commercial.models';
import { CommercialApiService } from '../../../core/services/commercial-api.service';
import { FinanceApiService } from '../../../core/services/finance-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

interface AllocationFormRow {
  arItemId: string;
  documentNumber: string;
  dueDate: string;
  outstandingPaise: number;
  allocatedPaise: number;
  isSelected: boolean;
}

@Component({
  selector: 'magic-customer-receipts',
  standalone: true,
  imports: [CommonModule, FormsModule, StatusBadgeComponent],
  templateUrl: './customer-receipts.component.html',
  styleUrl: './customer-receipts.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CustomerReceiptsComponent implements OnInit {
  private readonly api = inject(FinanceApiService);
  private readonly commercialApi = inject(CommercialApiService);

  protected readonly receipts = signal<readonly CustomerReceiptDto[]>([]);
  protected readonly selectedReceipt = signal<CustomerReceiptDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly isCreating = signal(false);
  protected readonly isCancelling = signal(false);

  // New Receipt Form State
  protected showCreateModal = false;
  protected customers = signal<readonly CustomerSummaryDto[]>([]);
  protected bankAccounts = signal<readonly BankCashAccountDto[]>([]);
  protected openArItems = signal<AllocationFormRow[]>([]);

  protected formCustomerId = '';
  protected formReceiptDate = new Date().toISOString().split('T')[0];
  protected formPaymentMethod = 'BankTransfer';
  protected formReferenceNumber = '';
  protected formBankAccountId = '';
  protected formTotalAmount = 0; // in Rupees
  protected formNarration = '';
  protected cancelReason = '';

  ngOnInit(): void {
    this.loadReceipts();
    this.loadPrerequisites();
  }

  protected loadReceipts(): void {
    this.loading.set(true);
    this.api.getReceipts().subscribe({
      next: (res) => {
        this.receipts.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadPrerequisites(): void {
    this.commercialApi.getCustomers().subscribe({
      next: (res) => this.customers.set(res.items),
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

  protected onCustomerSelected(): void {
    if (!this.formCustomerId) {
      this.openArItems.set([]);
      return;
    }

    this.api.getReceivables({ customerId: this.formCustomerId, status: 'OPEN' }).subscribe({
      next: (res) => {
        const rows: AllocationFormRow[] = res.items.map((i) => ({
          arItemId: i.id,
          documentNumber: i.documentNumber,
          dueDate: i.dueDate,
          outstandingPaise: i.outstandingAmountPaise,
          allocatedPaise: 0,
          isSelected: false,
        }));
        this.openArItems.set(rows);
      },
      error: () => {},
    });
  }

  protected autoAllocate(): void {
    let remainingPaise = Math.round(this.formTotalAmount * 100);
    const updated = this.openArItems().map((item) => {
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
    this.openArItems.set(updated);
  }

  protected calculateAllocatedPaise(): number {
    return this.openArItems()
      .filter((i) => i.isSelected)
      .reduce((sum, i) => sum + i.allocatedPaise, 0);
  }

  protected calculateUnallocatedPaise(): number {
    const totalPaise = Math.round(this.formTotalAmount * 100);
    return Math.max(0, totalPaise - this.calculateAllocatedPaise());
  }

  protected submitReceipt(): void {
    if (!this.formCustomerId || !this.formBankAccountId || this.formTotalAmount <= 0) {
      return;
    }

    this.isCreating.set(true);
    const totalPaise = Math.round(this.formTotalAmount * 100);
    const allocations = this.openArItems()
      .filter((i) => i.isSelected && i.allocatedPaise > 0)
      .map((i) => ({
        arItemId: i.arItemId,
        allocatedAmountPaise: i.allocatedPaise,
      }));

    this.api.createReceipt({
      customerId: this.formCustomerId,
      receiptDate: this.formReceiptDate,
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
        this.loadReceipts();
        this.selectedReceipt.set(created);
      },
      error: () => this.isCreating.set(false),
    });
  }

  protected confirmCancelReceipt(): void {
    const r = this.selectedReceipt();
    if (!r || !this.cancelReason) return;

    this.isCancelling.set(true);
    this.api.cancelReceipt(r.id, { reason: this.cancelReason }).subscribe({
      next: (reversed) => {
        this.isCancelling.set(false);
        this.selectedReceipt.set(reversed);
        this.loadReceipts();
      },
      error: () => this.isCancelling.set(false),
    });
  }

  protected printReceipt(): void {
    window.print();
  }

  protected formatPaise(paise: number): string {
    return (paise / 100).toLocaleString('en-IN', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
  }
}
