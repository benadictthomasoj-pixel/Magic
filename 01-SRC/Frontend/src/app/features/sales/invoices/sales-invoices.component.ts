import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { SalesInvoiceDto } from '../../../core/contracts/sales.models';
import { SalesApiService } from '../../../core/services/sales-api.service';

@Component({
  selector: 'app-sales-invoices',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './sales-invoices.component.html',
  styleUrls: ['./sales-invoices.component.scss'],
})
export class SalesInvoicesComponent implements OnInit {
  private readonly salesApi = inject(SalesApiService);

  invoices: SalesInvoiceDto[] = [];
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  // Filters
  searchTerm = '';
  statusFilter = '';
  fromDate = '';
  toDate = '';
  page = 1;
  pageSize = 20;
  totalCount = 0;

  // Invoice Detail Modal
  selectedInvoice: SalesInvoiceDto | null = null;
  showDetailModal = false;

  // Cancel Modal
  invoiceToCancel: SalesInvoiceDto | null = null;
  cancelReason = '';
  showCancelModal = false;
  isCancelling = false;

  // Reprint Receipt Modal
  reprintInvoice: SalesInvoiceDto | null = null;
  showReprintModal = false;
  receiptSize: '58mm' | '80mm' = '80mm';

  ngOnInit(): void {
    this.loadInvoices();
  }

  loadInvoices(): void {
    this.isLoading = true;
    this.errorMessage = '';

    this.salesApi
      .getInvoices({
        search: this.searchTerm || undefined,
        status: this.statusFilter || undefined,
        fromDate: this.fromDate ? new Date(this.fromDate).toISOString() : undefined,
        toDate: this.toDate ? new Date(this.toDate).toISOString() : undefined,
        page: this.page,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (res) => {
          this.invoices = [...res.items];
          this.totalCount = res.totalCount;
          this.isLoading = false;
        },
        error: (err) => {
          this.errorMessage = err.error?.message || err.message || 'Failed to load invoices.';
          this.isLoading = false;
        },
      });
  }

  onFilterChange(): void {
    this.page = 1;
    this.loadInvoices();
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.statusFilter = '';
    this.fromDate = '';
    this.toDate = '';
    this.page = 1;
    this.loadInvoices();
  }

  goToPage(p: number): void {
    if (p < 1 || p > this.totalPages) return;
    this.page = p;
    this.loadInvoices();
  }

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize) || 1;
  }

  get endItemIndex(): number {
    return Math.min(this.page * this.pageSize, this.totalCount);
  }

  viewDetails(inv: SalesInvoiceDto): void {
    this.selectedInvoice = inv;
    this.showDetailModal = true;
  }

  openCancelModal(inv: SalesInvoiceDto): void {
    this.invoiceToCancel = inv;
    this.cancelReason = '';
    this.showCancelModal = true;
  }

  confirmCancel(): void {
    if (!this.invoiceToCancel || !this.cancelReason.trim()) return;

    this.isCancelling = true;
    this.errorMessage = '';

    this.salesApi.cancelInvoice(this.invoiceToCancel.id, this.cancelReason.trim()).subscribe({
      next: (updated) => {
        this.isCancelling = false;
        this.showCancelModal = false;
        this.invoiceToCancel = null;
        this.successMessage = `Invoice ${updated.invoiceNumber} has been cancelled and physical stock restored.`;
        this.loadInvoices();
      },
      error: (err) => {
        this.isCancelling = false;
        this.errorMessage = err.error?.message || err.message || 'Cancellation failed.';
      },
    });
  }

  openReprintModal(inv: SalesInvoiceDto): void {
    this.reprintInvoice = inv;
    this.showReprintModal = true;
  }

  printReceipt(): void {
    window.print();
  }
}
