import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  CreateSalesReturnLineRequest,
  CreateSalesReturnRequest,
  CreditNoteDto,
  SalesInvoiceDto,
  SalesReturnDto,
} from '../../../core/contracts/sales.models';
import { SalesApiService } from '../../../core/services/sales-api.service';

interface ReturnLineFormItem {
  selected: boolean;
  invoiceLineId: string;
  productCode: string;
  productName: string;
  originalQuantity: number;
  unitPricePaise: number;
  gstRatePercentage: number;
  returnQuantity: number;
  isResalable: boolean;
  reason: string;
}

@Component({
  selector: 'app-sales-returns',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sales-returns.component.html',
  styleUrls: ['./sales-returns.component.scss'],
})
export class SalesReturnsComponent implements OnInit {
  private readonly salesApi = inject(SalesApiService);

  activeTab: 'history' | 'new-return' | 'credit-notes' = 'history';

  // Returns History
  returns: SalesReturnDto[] = [];
  isLoadingReturns = false;
  returnsSearch = '';
  returnsPage = 1;
  returnsPageSize = 20;
  returnsTotalCount = 0;

  // Credit Notes
  creditNotes: CreditNoteDto[] = [];
  isLoadingCreditNotes = false;
  creditNotesPage = 1;
  creditNotesPageSize = 20;
  creditNotesTotalCount = 0;

  // New Return Workflow
  invoiceSearchNumber = '';
  isSearchingInvoice = false;
  foundInvoice: SalesInvoiceDto | null = null;
  returnLines: ReturnLineFormItem[] = [];
  reasonCode = 'CustomerCancelled';
  returnNotes = '';
  isSubmittingReturn = false;

  // Messages
  errorMessage = '';
  successMessage = '';

  // Return Detail Modal
  selectedReturn: SalesReturnDto | null = null;
  showReturnDetailModal = false;

  ngOnInit(): void {
    this.loadReturns();
  }

  setTab(tab: 'history' | 'new-return' | 'credit-notes'): void {
    this.activeTab = tab;
    this.errorMessage = '';
    this.successMessage = '';

    if (tab === 'history') {
      this.loadReturns();
    } else if (tab === 'credit-notes') {
      this.loadCreditNotes();
    }
  }

  loadReturns(): void {
    this.isLoadingReturns = true;
    this.salesApi
      .getReturns({
        search: this.returnsSearch || undefined,
        page: this.returnsPage,
        pageSize: this.returnsPageSize,
      })
      .subscribe({
        next: (res) => {
          this.returns = [...res.items];
          this.returnsTotalCount = res.totalCount;
          this.isLoadingReturns = false;
        },
        error: (err) => {
          this.errorMessage = err.error?.message || err.message || 'Failed to load returns.';
          this.isLoadingReturns = false;
        },
      });
  }

  loadCreditNotes(): void {
    this.isLoadingCreditNotes = true;
    this.salesApi
      .getCreditNotes({
        page: this.creditNotesPage,
        pageSize: this.creditNotesPageSize,
      })
      .subscribe({
        next: (res) => {
          this.creditNotes = [...res.items];
          this.creditNotesTotalCount = res.totalCount;
          this.isLoadingCreditNotes = false;
        },
        error: (err) => {
          this.errorMessage = err.error?.message || err.message || 'Failed to load credit notes.';
          this.isLoadingCreditNotes = false;
        },
      });
  }

  // Invoice Search for Return
  searchInvoiceForReturn(): void {
    const term = this.invoiceSearchNumber.trim();
    if (!term) return;

    this.isSearchingInvoice = true;
    this.errorMessage = '';
    this.foundInvoice = null;
    this.returnLines = [];

    this.salesApi.getInvoices({ search: term, pageSize: 5 }).subscribe({
      next: (res) => {
        this.isSearchingInvoice = false;
        const match = res.items.find((i) => i.invoiceNumber.toLowerCase() === term.toLowerCase()) || res.items[0];
        if (match) {
          if (match.status === 'Cancelled') {
            this.errorMessage = `Invoice ${match.invoiceNumber} is cancelled and cannot be returned.`;
            return;
          }
          this.foundInvoice = match;
          this.returnLines = match.lines.map((l) => ({
            selected: true,
            invoiceLineId: l.id,
            productCode: l.productCode,
            productName: l.productName,
            originalQuantity: l.quantity,
            unitPricePaise: l.unitPricePaise,
            gstRatePercentage: l.gstRatePercentage,
            returnQuantity: 1,
            isResalable: true,
            reason: 'Item returned in resalable condition',
          }));
        } else {
          this.errorMessage = `No sales invoice found with number '${term}'.`;
        }
      },
      error: (err) => {
        this.isSearchingInvoice = false;
        this.errorMessage = err.error?.message || err.message || 'Invoice lookup failed.';
      },
    });
  }

  get selectedReturnLines(): ReturnLineFormItem[] {
    return this.returnLines.filter((l) => l.selected && l.returnQuantity > 0);
  }

  get calculatedRefundPaise(): number {
    return this.selectedReturnLines.reduce((sum, l) => {
      const lineTax = Math.round((l.unitPricePaise * l.returnQuantity * l.gstRatePercentage) / 100);
      return sum + l.unitPricePaise * l.returnQuantity + lineTax;
    }, 0);
  }

  submitSalesReturn(): void {
    if (!this.foundInvoice) return;
    if (this.selectedReturnLines.length === 0) {
      this.errorMessage = 'Please select at least one line item to return.';
      return;
    }

    for (const line of this.selectedReturnLines) {
      if (line.returnQuantity > line.originalQuantity) {
        this.errorMessage = `Return quantity for ${line.productName} cannot exceed original sold quantity (${line.originalQuantity}).`;
        return;
      }
    }

    this.isSubmittingReturn = true;
    this.errorMessage = '';

    const lines: CreateSalesReturnLineRequest[] = this.selectedReturnLines.map((l) => ({
      invoiceLineId: l.invoiceLineId,
      returnQuantity: l.returnQuantity,
      isResalable: l.isResalable,
      reason: l.reason || 'Customer return',
    }));

    const payload: CreateSalesReturnRequest = {
      invoiceId: this.foundInvoice.id,
      reasonCode: this.reasonCode,
      notes: this.returnNotes || null,
      lines,
    };

    this.salesApi.createReturn(payload).subscribe({
      next: (created) => {
        this.isSubmittingReturn = false;
        this.successMessage = `Sales Return ${created.returnNumber} confirmed! Credit Note ${created.creditNoteNumber} issued.`;
        this.foundInvoice = null;
        this.returnLines = [];
        this.invoiceSearchNumber = '';
        this.setTab('history');
      },
      error: (err) => {
        this.isSubmittingReturn = false;
        this.errorMessage = err.error?.message || err.message || 'Failed to process return.';
      },
    });
  }

  viewReturnDetails(ret: SalesReturnDto): void {
    this.selectedReturn = ret;
    this.showReturnDetailModal = true;
  }
}
