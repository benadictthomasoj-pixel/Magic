import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  HourlySalesDto,
  SalesByPaymentMethodDto,
  SalesByProductDto,
  SalesReportSummaryDto,
} from '../../../core/contracts/sales.models';
import { SalesApiService } from '../../../core/services/sales-api.service';

@Component({
  selector: 'app-sales-reports',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './sales-reports.component.html',
  styleUrls: ['./sales-reports.component.scss'],
})
export class SalesReportsComponent implements OnInit {
  private readonly salesApi = inject(SalesApiService);

  fromDate = '';
  toDate = '';
  activePreset: 'today' | 'yesterday' | '7days' | '30days' = '30days';

  summary: SalesReportSummaryDto | null = null;
  paymentMethods: readonly SalesByPaymentMethodDto[] = [];
  topProducts: readonly SalesByProductDto[] = [];
  hourlySales: readonly HourlySalesDto[] = [];

  isLoading = false;
  errorMessage = '';

  ngOnInit(): void {
    this.applyPreset('30days');
  }

  applyPreset(preset: 'today' | 'yesterday' | '7days' | '30days'): void {
    this.activePreset = preset;
    const now = new Date();
    const end = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 23, 59, 59);
    let start = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 0, 0, 0);

    if (preset === 'today') {
      // today start to end
    } else if (preset === 'yesterday') {
      start.setDate(start.getDate() - 1);
      end.setDate(end.getDate() - 1);
    } else if (preset === '7days') {
      start.setDate(start.getDate() - 7);
    } else if (preset === '30days') {
      start.setDate(start.getDate() - 30);
    }

    this.fromDate = this.formatDate(start);
    this.toDate = this.formatDate(end);
    this.loadAllReports();
  }

  private formatDate(d: Date): string {
    return d.toISOString().split('T')[0];
  }

  loadAllReports(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const fromIso = this.fromDate ? new Date(this.fromDate).toISOString() : undefined;
    const toIso = this.toDate ? new Date(`${this.toDate}T23:59:59.999Z`).toISOString() : undefined;

    this.salesApi.getSummary({ fromDate: fromIso, toDate: toIso }).subscribe({
      next: (sum) => (this.summary = sum),
      error: (err) => (this.errorMessage = err.message),
    });

    this.salesApi.getPaymentBreakdown({ fromDate: fromIso, toDate: toIso }).subscribe({
      next: (pays) => (this.paymentMethods = pays),
    });

    this.salesApi.getTopProducts({ fromDate: fromIso, toDate: toIso, limit: 10 }).subscribe({
      next: (prods) => (this.topProducts = prods),
    });

    this.salesApi.getHourlySales({ date: fromIso }).subscribe({
      next: (hrs) => {
        this.hourlySales = hrs;
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      },
    });
  }

  get maxHourlySales(): number {
    return Math.max(1, ...this.hourlySales.map((h) => h.totalSalesPaise));
  }
}
