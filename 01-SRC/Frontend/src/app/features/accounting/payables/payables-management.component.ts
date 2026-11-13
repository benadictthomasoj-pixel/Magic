import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  CreateSupplierBillLineRequest,
  PayablesSummaryDto,
  SupplierAgingReportDto,
  SupplierPayableDto,
  SupplierStatementDto,
} from '../../../core/contracts/finance.models';
import { SupplierSummaryDto } from '../../../core/contracts/commercial.models';
import { CommercialApiService } from '../../../core/services/commercial-api.service';
import { FinanceApiService } from '../../../core/services/finance-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

interface BillFormLine {
  description: string;
  quantity: number;
  unitPrice: number; // in Rupees
  taxRate: number;
}

@Component({
  selector: 'magic-payables-management',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, StatusBadgeComponent],
  templateUrl: './payables-management.component.html',
  styleUrl: './payables-management.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PayablesManagementComponent implements OnInit {
  private readonly api = inject(FinanceApiService);
  private readonly commercialApi = inject(CommercialApiService);

  protected readonly activeTab = signal<'bills' | 'aging'>('bills');
  protected readonly summary = signal<PayablesSummaryDto | null>(null);
  protected readonly payables = signal<readonly SupplierPayableDto[]>([]);
  protected readonly agingReport = signal<SupplierAgingReportDto | null>(null);
  protected readonly selectedStatement = signal<SupplierStatementDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly isCreatingBill = signal(false);

  // New Supplier Bill Modal
  protected showBillModal = false;
  protected suppliers = signal<readonly SupplierSummaryDto[]>([]);
  protected billSupplierId = '';
  protected billInvoiceNumber = '';
  protected billInvoiceDate = new Date().toISOString().split('T')[0];
  protected billDueDate = new Date(Date.now() + 30 * 86400000).toISOString().split('T')[0];
  protected billNotes = '';
  protected billLines: BillFormLine[] = [
    { description: '', quantity: 1, unitPrice: 0, taxRate: 18 },
  ];

  protected filterStatus = '';
  protected filterOverdueOnly = false;
  protected agingReportDate = new Date().toISOString().split('T')[0];

  ngOnInit(): void {
    this.loadSummary();
    this.loadPayables();
    this.loadSuppliers();
  }

  protected setTab(tab: 'bills' | 'aging'): void {
    this.activeTab.set(tab);
    if (tab === 'aging' && !this.agingReport()) {
      this.loadAging();
    }
  }

  protected loadSummary(): void {
    this.api.getPayablesSummary().subscribe({
      next: (res) => this.summary.set(res),
      error: () => {},
    });
  }

  protected loadPayables(): void {
    this.loading.set(true);
    this.api.getPayables({
      status: this.filterStatus || undefined,
      overdueOnly: this.filterOverdueOnly || undefined,
    }).subscribe({
      next: (res) => {
        this.payables.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadAging(): void {
    this.loading.set(true);
    this.api.getSupplierAging(this.agingReportDate).subscribe({
      next: (res) => {
        this.agingReport.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadSuppliers(): void {
    this.commercialApi.getSuppliers().subscribe({
      next: (res) => this.suppliers.set(res.items),
      error: () => {},
    });
  }

  protected addBillLine(): void {
    this.billLines.push({ description: '', quantity: 1, unitPrice: 0, taxRate: 18 });
  }

  protected removeBillLine(index: number): void {
    if (this.billLines.length > 1) {
      this.billLines.splice(index, 1);
    }
  }

  protected calculateSubtotal(): number {
    return this.billLines.reduce((sum, l) => sum + (l.quantity * l.unitPrice), 0);
  }

  protected calculateTax(): number {
    return this.billLines.reduce((sum, l) => sum + (l.quantity * l.unitPrice * (l.taxRate / 100)), 0);
  }

  protected calculateTotal(): number {
    return this.calculateSubtotal() + this.calculateTax();
  }

  protected submitSupplierBill(): void {
    if (!this.billSupplierId || !this.billInvoiceNumber || this.billLines.length === 0) {
      return;
    }

    this.isCreatingBill.set(true);
    const linesReq: CreateSupplierBillLineRequest[] = this.billLines.map((l) => ({
      description: l.description.trim() || 'Commercial supplies',
      quantity: l.quantity,
      unitPricePaise: Math.round(l.unitPrice * 100),
      taxRatePercentage: l.taxRate,
    }));

    this.api.createSupplierBill({
      supplierId: this.billSupplierId,
      invoiceNumber: this.billInvoiceNumber.trim(),
      invoiceDate: this.billInvoiceDate,
      dueDate: this.billDueDate,
      notes: this.billNotes || undefined,
      lines: linesReq,
    }).subscribe({
      next: () => {
        this.isCreatingBill.set(false);
        this.showBillModal = false;
        this.billLines = [{ description: '', quantity: 1, unitPrice: 0, taxRate: 18 }];
        this.billInvoiceNumber = '';
        this.loadSummary();
        this.loadPayables();
      },
      error: () => this.isCreatingBill.set(false),
    });
  }

  protected viewStatement(supplierId: string): void {
    this.api.getSupplierStatement(supplierId).subscribe({
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
