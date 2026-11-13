import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  EmployeeSalaryAssignmentDto,
  PayrollItemDto,
  PayrollPeriodDto,
  PayrollSummaryDto,
  SalaryStructureDto,
} from '../../../core/contracts/operations.models';
import { OperationsApiService } from '../../../core/services/operations-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'magic-payroll-management',
  standalone: true,
  imports: [CommonModule, FormsModule, StatusBadgeComponent],
  templateUrl: './payroll-management.component.html',
  styleUrl: './payroll-management.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PayrollManagementComponent implements OnInit {
  private readonly api = inject(OperationsApiService);

  protected readonly activeTab = signal<'periods' | 'structures' | 'payslip'>('periods');
  protected readonly summary = signal<PayrollSummaryDto | null>(null);
  protected readonly periods = signal<readonly PayrollPeriodDto[]>([]);
  protected readonly selectedPeriod = signal<PayrollPeriodDto | null>(null);
  protected readonly structures = signal<readonly SalaryStructureDto[]>([]);
  protected readonly selectedPayslip = signal<PayrollItemDto | null>(null);
  protected readonly loading = signal(true);

  protected readonly showCalculateModal = signal(false);
  protected readonly showDisburseModal = signal(false);

  protected calcMonth = new Date().getMonth() + 1;
  protected calcYear = new Date().getFullYear();

  protected disbursePeriodId = '';
  protected disburseData = {
    sourceBankAccountId: '11111111-1111-1111-1111-111111111111',
    paymentReference: 'NEFT-SALARY',
    disbursementDate: new Date().toISOString().split('T')[0],
  };

  ngOnInit(): void {
    this.loadSummary();
    this.loadPeriods();
    this.loadStructures();
  }

  protected setTab(tab: 'periods' | 'structures' | 'payslip'): void {
    this.activeTab.set(tab);
  }

  protected loadSummary(): void {
    this.api.getPayrollSummary().subscribe({
      next: (res) => this.summary.set(res),
      error: () => {},
    });
  }

  protected loadPeriods(): void {
    this.loading.set(true);
    this.api.getPayrollPeriods().subscribe({
      next: (res) => {
        this.periods.set(res.items);
        if (res.items.length > 0 && !this.selectedPeriod()) {
          this.selectPeriod(res.items[0]);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadStructures(): void {
    this.api.getSalaryStructures().subscribe({
      next: (res) => this.structures.set(res),
      error: () => {},
    });
  }

  protected selectPeriod(period: PayrollPeriodDto): void {
    this.api.getPayrollPeriodById(period.id).subscribe({
      next: (res) => this.selectedPeriod.set(res),
      error: () => this.selectedPeriod.set(period),
    });
  }

  protected calculatePayroll(): void {
    this.api.calculatePayroll(this.calcMonth, this.calcYear).subscribe({
      next: (res) => {
        this.showCalculateModal.set(false);
        this.loadPeriods();
        this.loadSummary();
        this.selectPeriod(res);
      },
    });
  }

  protected approvePayroll(id: string): void {
    this.api.approvePayroll(id).subscribe({
      next: (res) => {
        this.loadPeriods();
        this.loadSummary();
        this.selectedPeriod.set(res);
      },
    });
  }

  protected finalizePayroll(id: string): void {
    this.api.finalizePayroll(id).subscribe({
      next: (res) => {
        this.loadPeriods();
        this.loadSummary();
        this.selectedPeriod.set(res);
      },
    });
  }

  protected openDisburseModal(id: string): void {
    this.disbursePeriodId = id;
    this.showDisburseModal.set(true);
  }

  protected disbursePayroll(): void {
    if (!this.disbursePeriodId) return;
    this.api.disbursePayroll(this.disbursePeriodId, this.disburseData).subscribe({
      next: (res) => {
        this.showDisburseModal.set(false);
        this.loadPeriods();
        this.loadSummary();
        this.selectedPeriod.set(res);
      },
    });
  }

  protected viewPayslip(item: PayrollItemDto): void {
    this.api.getPayslip(item.id).subscribe({
      next: (res) => {
        this.selectedPayslip.set(res);
        this.activeTab.set('payslip');
      },
    });
  }

  protected formatPaise(paise?: number): string {
    if (paise == null) return '0.00';
    return (paise / 100).toLocaleString('en-IN', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
  }
}

