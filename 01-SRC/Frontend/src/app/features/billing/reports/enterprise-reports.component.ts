import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  EnterpriseApiService,
  ExecutiveDashboard,
  ReportResult,
  GenericReportRow,
} from '../../../core/services/enterprise-api.service';

@Component({
  selector: 'magic-enterprise-reports',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './enterprise-reports.component.html',
  styleUrl: './enterprise-reports.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EnterpriseReportsComponent implements OnInit {
  private readonly api = inject(EnterpriseApiService);

  protected readonly activeTab = signal<'executive' | 'sales' | 'procurement' | 'inventory' | 'financial' | 'hr'>('executive');
  protected readonly dashboard = signal<ExecutiveDashboard | null>(null);
  protected readonly reportData = signal<ReportResult | null>(null);
  protected readonly loading = signal(false);
  protected readonly exporting = signal(false);
  protected readonly exportMessage = signal<string | null>(null);

  // Filters
  protected fromDate = new Date(new Date().getFullYear(), new Date().getMonth(), 1).toISOString().split('T')[0];
  protected toDate = new Date().toISOString().split('T')[0];
  protected selectedBranchId = '';
  protected currentReportType = 'sales-by-period';

  ngOnInit(): void {
    this.loadExecutiveDashboard();
  }

  protected setTab(tab: 'executive' | 'sales' | 'procurement' | 'inventory' | 'financial' | 'hr'): void {
    this.activeTab.set(tab);
    if (tab === 'executive') {
      this.loadExecutiveDashboard();
    } else {
      // Set default report type for selected tab
      switch (tab) {
        case 'sales':
          this.currentReportType = 'sales-by-period';
          break;
        case 'procurement':
          this.currentReportType = 'purchases-by-supplier';
          break;
        case 'inventory':
          this.currentReportType = 'inventory-summary';
          break;
        case 'financial':
          this.currentReportType = 'receivables-aging';
          break;
        case 'hr':
          this.currentReportType = 'payroll-summary';
          break;
      }
      this.loadReport();
    }
  }

  protected loadExecutiveDashboard(): void {
    this.loading.set(true);
    this.api.getExecutiveDashboard(this.fromDate, this.toDate).subscribe({
      next: (res) => {
        this.dashboard.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadReport(): void {
    this.loading.set(true);
    this.api
      .getReport({
        reportType: this.currentReportType,
        fromDate: this.fromDate,
        toDate: this.toDate,
        branchId: this.selectedBranchId || undefined,
      })
      .subscribe({
        next: (res) => {
          this.reportData.set(res);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  protected exportReport(format: 'csv' | 'pdf' | 'xlsx'): void {
    this.exporting.set(true);
    this.exportMessage.set(null);
    this.api
      .exportReport({
        reportType: this.currentReportType,
        format,
        fromDate: this.fromDate,
        toDate: this.toDate,
        branchId: this.selectedBranchId || undefined,
      })
      .subscribe({
        next: (res) => {
          this.exporting.set(false);
          this.exportMessage.set(`Report successfully exported: ${res.fileName} (${(res.fileSizeBytes / 1024).toFixed(1)} KB)`);
          setTimeout(() => this.exportMessage.set(null), 5000);
        },
        error: (err) => {
          this.exporting.set(false);
          this.exportMessage.set(`Export failed: ${err.message || 'Unknown error'}`);
        },
      });
  }

  protected formatPaise(paise?: number): string {
    if (paise === undefined || paise === null) return '0.00';
    return (paise / 100).toLocaleString('en-IN', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
  }
}
