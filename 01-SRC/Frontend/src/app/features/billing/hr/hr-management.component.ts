import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  AttendanceDto,
  DesignationDto,
  EmployeeDto,
  HrSummaryDto,
  LeaveApplicationDto,
  LeaveTypeDto,
} from '../../../core/contracts/operations.models';
import { OperationsApiService } from '../../../core/services/operations-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'magic-hr-management',
  standalone: true,
  imports: [CommonModule, FormsModule, StatusBadgeComponent],
  templateUrl: './hr-management.component.html',
  styleUrl: './hr-management.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HrManagementComponent implements OnInit {
  private readonly api = inject(OperationsApiService);

  protected readonly activeTab = signal<'employees' | 'attendance' | 'leaves'>('employees');
  protected readonly summary = signal<HrSummaryDto | null>(null);
  protected readonly employees = signal<readonly EmployeeDto[]>([]);
  protected readonly designations = signal<readonly DesignationDto[]>([]);
  protected readonly attendanceList = signal<readonly AttendanceDto[]>([]);
  protected readonly leaveApplications = signal<readonly LeaveApplicationDto[]>([]);
  protected readonly leaveTypes = signal<readonly LeaveTypeDto[]>([]);
  protected readonly loading = signal(true);

  protected readonly showNewEmployeeModal = signal(false);
  protected readonly showLeaveModal = signal(false);

  protected attendanceDate = new Date().toISOString().split('T')[0];
  protected searchQuery = '';

  protected newEmployee = {
    employeeCode: `EMP-${Math.floor(1000 + Math.random() * 9000)}`,
    firstName: '',
    lastName: '',
    email: '',
    phone: '',
    joiningDate: new Date().toISOString().split('T')[0],
  };

  protected newLeave = {
    employeeId: '',
    leaveTypeId: '',
    startDate: new Date().toISOString().split('T')[0],
    endDate: new Date().toISOString().split('T')[0],
    reason: '',
  };

  ngOnInit(): void {
    this.loadSummary();
    this.loadEmployees();
    this.loadDesignations();
    this.loadLeaveTypes();
  }

  protected setTab(tab: 'employees' | 'attendance' | 'leaves'): void {
    this.activeTab.set(tab);
    if (tab === 'attendance') {
      this.loadAttendance();
    } else if (tab === 'leaves') {
      this.loadLeaves();
    }
  }

  protected loadSummary(): void {
    this.api.getHrSummary().subscribe({
      next: (res) => this.summary.set(res),
      error: () => {},
    });
  }

  protected loadEmployees(): void {
    this.loading.set(true);
    this.api.getEmployees({ search: this.searchQuery || undefined }).subscribe({
      next: (res) => {
        this.employees.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadDesignations(): void {
    this.api.getDesignations().subscribe({
      next: (res) => this.designations.set(res),
      error: () => {},
    });
  }

  protected loadLeaveTypes(): void {
    this.api.getLeaveTypes().subscribe({
      next: (res) => {
        this.leaveTypes.set(res);
        if (res.length > 0) this.newLeave.leaveTypeId = res[0].id;
      },
      error: () => {},
    });
  }

  protected loadAttendance(): void {
    this.loading.set(true);
    this.api.getAttendance(this.attendanceDate).subscribe({
      next: (res) => {
        this.attendanceList.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadLeaves(): void {
    this.loading.set(true);
    this.api.getLeaveApplications().subscribe({
      next: (res) => {
        this.leaveApplications.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected submitEmployee(): void {
    if (!this.newEmployee.firstName || !this.newEmployee.email) return;

    this.api
      .createEmployee({
        employeeCode: this.newEmployee.employeeCode,
        firstName: this.newEmployee.firstName,
        lastName: this.newEmployee.lastName,
        email: this.newEmployee.email,
        phone: this.newEmployee.phone || undefined,
        joiningDate: this.newEmployee.joiningDate,
      })
      .subscribe({
        next: () => {
          this.showNewEmployeeModal.set(false);
          this.loadEmployees();
          this.loadSummary();
        },
      });
  }

  protected punchAttendance(empId: string, type: 'In' | 'Out'): void {
    this.api
      .punchAttendance({
        employeeId: empId,
        attendanceDate: this.attendanceDate,
        punchType: type,
        timestampUtc: new Date().toISOString(),
      })
      .subscribe({
        next: () => {
          this.loadAttendance();
          this.loadSummary();
        },
      });
  }

  protected submitLeave(): void {
    if (!this.newLeave.employeeId || !this.newLeave.leaveTypeId) return;

    this.api.submitLeaveApplication(this.newLeave).subscribe({
      next: () => {
        this.showLeaveModal.set(false);
        this.loadLeaves();
        this.loadSummary();
      },
    });
  }

  protected approveLeave(id: string): void {
    this.api.approveLeaveApplication(id).subscribe({
      next: () => {
        this.loadLeaves();
        this.loadSummary();
      },
    });
  }

  protected rejectLeave(id: string): void {
    const reason = prompt('Please enter rejection reason:');
    if (!reason) return;
    this.api.rejectLeaveApplication(id, reason).subscribe({
      next: () => {
        this.loadLeaves();
        this.loadSummary();
      },
    });
  }
}

