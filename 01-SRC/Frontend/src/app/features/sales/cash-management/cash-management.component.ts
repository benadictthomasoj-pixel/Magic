import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import {
  CashMovementDto,
  CashierSessionDto,
  CloseCashierSessionRequest,
  OpenCashierSessionRequest,
  RecordCashMovementRequest,
} from '../../../core/contracts/sales.models';
import { SalesApiService } from '../../../core/services/sales-api.service';

@Component({
  selector: 'app-cash-management',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './cash-management.component.html',
  styleUrls: ['./cash-management.component.scss'],
})
export class CashManagementComponent implements OnInit {
  private readonly salesApi = inject(SalesApiService);

  activeSession: CashierSessionDto | null = null;
  sessions: CashierSessionDto[] = [];
  isLoading = false;
  errorMessage = '';
  successMessage = '';

  // Open Shift Modal
  showOpenModal = false;
  openingFloatRupees = 5000;
  isOpening = false;

  // Cash Movement Modal (Drop/Deposit)
  showMovementModal = false;
  movementType: 'Withdrawal' | 'Deposit' = 'Withdrawal';
  movementAmountRupees = 1000;
  movementReason = 'Safe Drop';
  isRecordingMovement = false;

  // Close Shift Modal
  showCloseModal = false;
  actualCountRupees = 0;
  closingNotes = '';
  isClosing = false;

  // Session Details Modal
  selectedSession: CashierSessionDto | null = null;
  showDetailsModal = false;

  ngOnInit(): void {
    this.refreshAll();
  }

  refreshAll(): void {
    this.loadActiveSession();
    this.loadSessionsHistory();
  }

  loadActiveSession(): void {
    this.salesApi.getActiveSession().subscribe({
      next: (session) => {
        this.activeSession = session;
      },
    });
  }

  loadSessionsHistory(): void {
    this.isLoading = true;
    this.salesApi.getSessions({ pageSize: 30 }).subscribe({
      next: (res) => {
        this.sessions = [...res.items];
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.error?.message || err.message || 'Failed to load cashier history.';
        this.isLoading = false;
      },
    });
  }

  // Open Shift
  openShift(): void {
    this.isOpening = true;
    this.errorMessage = '';

    const req: OpenCashierSessionRequest = {
      openingCashPaise: Math.round(Number(this.openingFloatRupees || 0) * 100),
    };

    this.salesApi.openSession(req).subscribe({
      next: (session) => {
        this.isOpening = false;
        this.showOpenModal = false;
        this.successMessage = `Cashier shift opened successfully with ₹${this.openingFloatRupees.toFixed(2)} float!`;
        this.activeSession = session;
        this.loadSessionsHistory();
      },
      error: (err) => {
        this.isOpening = false;
        this.errorMessage = err.error?.message || err.message || 'Failed to open shift.';
      },
    });
  }

  // Record Movement
  openMovementModal(type: 'Withdrawal' | 'Deposit'): void {
    this.movementType = type;
    this.movementAmountRupees = type === 'Withdrawal' ? 1000 : 500;
    this.movementReason = type === 'Withdrawal' ? 'Mid-shift safe deposit drop' : 'Cash float replenishment';
    this.showMovementModal = true;
  }

  recordMovement(): void {
    if (!this.activeSession) return;
    this.isRecordingMovement = true;
    this.errorMessage = '';

    const req: RecordCashMovementRequest = {
      movementType: this.movementType,
      amountPaise: Math.round(Number(this.movementAmountRupees || 0) * 100),
      reason: this.movementReason.trim(),
    };

    this.salesApi.recordMovement(this.activeSession.id, req).subscribe({
      next: () => {
        this.isRecordingMovement = false;
        this.showMovementModal = false;
        this.successMessage = `Recorded ${this.movementType} of ₹${this.movementAmountRupees.toFixed(2)}.`;
        this.refreshAll();
      },
      error: (err) => {
        this.isRecordingMovement = false;
        this.errorMessage = err.error?.message || err.message || 'Cash movement failed.';
      },
    });
  }

  // Close Shift
  initiateClosing(): void {
    if (!this.activeSession) return;
    this.actualCountRupees = this.activeSession.expectedCashPaise / 100;
    this.closingNotes = 'Day-end cash drawer counted and verified';
    this.showCloseModal = true;
  }

  get closeVarianceRupees(): number {
    if (!this.activeSession) return 0;
    return this.actualCountRupees - this.activeSession.expectedCashPaise / 100;
  }

  confirmCloseShift(): void {
    if (!this.activeSession) return;
    this.isClosing = true;
    this.errorMessage = '';

    const req: CloseCashierSessionRequest = {
      actualClosingCashPaise: Math.round(Number(this.actualCountRupees || 0) * 100),
      notes: this.closingNotes.trim() || undefined,
    };

    this.salesApi.closeSession(this.activeSession.id, req).subscribe({
      next: (closed) => {
        this.isClosing = false;
        this.showCloseModal = false;
        this.activeSession = null;
        this.successMessage = `Shift closed. Variance: ₹${((closed.variancePaise ?? 0) / 100).toFixed(2)}.`;
        this.refreshAll();
      },
      error: (err) => {
        this.isClosing = false;
        this.errorMessage = err.error?.message || err.message || 'Failed to close shift.';
      },
    });
  }

  viewSession(session: CashierSessionDto): void {
    this.selectedSession = session;
    this.showDetailsModal = true;
  }
}
