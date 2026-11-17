import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnInit, inject, signal } from '@angular/core';
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
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CashManagementComponent implements OnInit {
  private readonly salesApi = inject(SalesApiService);
  private readonly cdr = inject(ChangeDetectorRef);

  readonly activeSession = signal<CashierSessionDto | null>(null);
  readonly sessions = signal<CashierSessionDto[]>([]);
  readonly isLoading = signal<boolean>(false);
  readonly errorMessage = signal<string>('');
  readonly successMessage = signal<string>('');

  // Open Shift Modal
  readonly showOpenModal = signal<boolean>(false);
  openingFloatRupees: number = 5000;
  readonly isOpening = signal<boolean>(false);

  // Cash Movement Modal (Drop/Deposit)
  readonly showMovementModal = signal<boolean>(false);
  movementType: 'Withdrawal' | 'Deposit' = 'Withdrawal';
  movementAmountRupees: number = 1000;
  movementReason: string = 'Safe Drop';
  readonly isRecordingMovement = signal<boolean>(false);

  // Close Shift Modal
  readonly showCloseModal = signal<boolean>(false);
  actualCountRupees: number = 0;
  closingNotes: string = '';
  readonly isClosing = signal<boolean>(false);

  // Session Details Modal
  readonly selectedSession = signal<CashierSessionDto | null>(null);
  readonly showDetailsModal = signal<boolean>(false);

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
        this.activeSession.set(session);
        this.cdr.markForCheck();
      },
      error: (err) => {
        const detail = err.error?.error?.detail || err.error?.detail || err.message || 'Failed to check active cashier shift.';
        this.errorMessage.set(detail);
        this.cdr.markForCheck();
      },
    });
  }

  loadSessionsHistory(): void {
    this.isLoading.set(true);
    this.salesApi.getSessions({ pageSize: 30 }).subscribe({
      next: (res) => {
        this.sessions.set([...res.items]);
        this.isLoading.set(false);
        this.cdr.markForCheck();
      },
      error: (err) => {
        const detail = err.error?.error?.detail || err.error?.detail || err.message || 'Failed to load cashier history.';
        this.errorMessage.set(detail);
        this.isLoading.set(false);
        this.cdr.markForCheck();
      },
    });
  }

  // Open Shift
  openShift(): void {
    if (this.isOpening()) return;
    this.isOpening.set(true);
    this.errorMessage.set('');

    const floatRupees = Number(this.openingFloatRupees) || 0;
    const req: OpenCashierSessionRequest = {
      openingCashPaise: Math.round(floatRupees * 100),
    };

    this.salesApi.openSession(req).subscribe({
      next: (session) => {
        this.isOpening.set(false);
        this.showOpenModal.set(false);
        this.successMessage.set(`Cashier shift opened successfully with ₹${floatRupees.toFixed(2)} float!`);
        this.activeSession.set(session);
        this.loadSessionsHistory();
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isOpening.set(false);
        const detail = err.error?.error?.detail || err.error?.detail || err.error?.message || err.message || 'Failed to open cashier shift.';
        this.errorMessage.set(detail);
        this.cdr.markForCheck();
      },
    });
  }

  openOpenModal(): void {
    this.openingFloatRupees = 5000;
    this.errorMessage.set('');
    this.showOpenModal.set(true);
  }

  closeOpenModal(): void {
    if (!this.isOpening()) {
      this.showOpenModal.set(false);
    }
  }

  // Record Movement
  openMovementModal(type: 'Withdrawal' | 'Deposit'): void {
    this.movementType = type;
    this.movementAmountRupees = type === 'Withdrawal' ? 1000 : 500;
    this.movementReason = type === 'Withdrawal' ? 'Mid-shift safe deposit drop' : 'Cash float replenishment';
    this.showMovementModal.set(true);
  }

  closeMovementModal(): void {
    if (!this.isRecordingMovement()) {
      this.showMovementModal.set(false);
    }
  }

  recordMovement(): void {
    const session = this.activeSession();
    if (!session || this.isRecordingMovement()) return;
    this.isRecordingMovement.set(true);
    this.errorMessage.set('');

    const amountRupees = Number(this.movementAmountRupees) || 0;
    const req: RecordCashMovementRequest = {
      movementType: this.movementType,
      amountPaise: Math.round(amountRupees * 100),
      reason: (this.movementReason || '').trim(),
    };

    this.salesApi.recordMovement(session.id, req).subscribe({
      next: () => {
        this.isRecordingMovement.set(false);
        this.showMovementModal.set(false);
        this.successMessage.set(`Recorded ${this.movementType} of ₹${amountRupees.toFixed(2)}.`);
        this.refreshAll();
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isRecordingMovement.set(false);
        const detail = err.error?.error?.detail || err.error?.detail || err.message || 'Cash movement failed.';
        this.errorMessage.set(detail);
        this.cdr.markForCheck();
      },
    });
  }

  // Close Shift
  initiateClosing(): void {
    const session = this.activeSession();
    if (!session) return;
    this.actualCountRupees = session.expectedCashPaise / 100;
    this.closingNotes = 'Day-end cash drawer counted and verified';
    this.showCloseModal.set(true);
  }

  closeCloseModal(): void {
    if (!this.isClosing()) {
      this.showCloseModal.set(false);
    }
  }

  get closeVarianceRupees(): number {
    const session = this.activeSession();
    if (!session) return 0;
    return (Number(this.actualCountRupees) || 0) - session.expectedCashPaise / 100;
  }

  confirmCloseShift(): void {
    const session = this.activeSession();
    if (!session || this.isClosing()) return;
    this.isClosing.set(true);
    this.errorMessage.set('');

    const countRupees = Number(this.actualCountRupees) || 0;
    const req: CloseCashierSessionRequest = {
      actualClosingCashPaise: Math.round(countRupees * 100),
      notes: (this.closingNotes || '').trim() || undefined,
    };

    this.salesApi.closeSession(session.id, req).subscribe({
      next: (closed) => {
        this.isClosing.set(false);
        this.showCloseModal.set(false);
        this.activeSession.set(null);
        this.successMessage.set(`Shift closed. Variance: ₹${((closed.variancePaise ?? 0) / 100).toFixed(2)}.`);
        this.refreshAll();
        this.cdr.markForCheck();
      },
      error: (err) => {
        this.isClosing.set(false);
        const detail = err.error?.error?.detail || err.error?.detail || err.message || 'Failed to close shift.';
        this.errorMessage.set(detail);
        this.cdr.markForCheck();
      },
    });
  }

  viewSession(session: CashierSessionDto): void {
    this.selectedSession.set(session);
    this.showDetailsModal.set(true);
  }

  closeDetailsModal(): void {
    this.showDetailsModal.set(false);
  }

  clearSuccess(): void {
    this.successMessage.set('');
  }

  clearError(): void {
    this.errorMessage.set('');
  }
}
