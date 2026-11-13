import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  CreateStockTransferRequest,
  DispatchStockTransferRequest,
  ReceiveStockTransferRequest,
  StockTransferDto,
  WarehouseDto,
} from '../../../core/contracts/inventory.models';
import { InventoryApiService } from '../../../core/services/inventory-api.service';

@Component({
  selector: 'magic-stock-transfers',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './stock-transfers.component.html',
  styleUrl: './stock-transfers.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StockTransfersComponent implements OnInit {
  private readonly api = inject(InventoryApiService);

  protected readonly transfers = signal<readonly StockTransferDto[]>([]);
  protected readonly warehouses = signal<readonly WarehouseDto[]>([]);
  protected readonly selectedTransfer = signal<StockTransferDto | null>(null);
  protected readonly isCreateOpen = signal<boolean>(false);
  protected readonly isDetailOpen = signal<boolean>(false);
  protected readonly isDispatchOpen = signal<boolean>(false);
  protected readonly isReceiveOpen = signal<boolean>(false);
  protected readonly loading = signal<boolean>(true);
  protected readonly saving = signal<boolean>(false);
  protected readonly actionMessage = signal<{ type: 'ok' | 'error'; text: string } | null>(null);

  protected filterStatus = '';
  protected currentPage = 1;
  protected pageSize = 20;
  protected totalTransfers = 0;

  // Create form
  protected formSourceWhId = '';
  protected formDestWhId = '';
  protected formNotes = '';
  protected formLines: Array<{
    productId: string;
    requestedQuantity: number;
  }> = [];

  // Dispatch modal
  protected dispatchCarrier = '';
  protected dispatchTracking = '';

  // Receive modal: lineId -> receivedQty
  protected receivedQuantities: Record<string, number> = {};
  protected damagedQuantities: Record<string, number> = {};

  ngOnInit(): void {
    this.loadWarehouses();
    this.loadTransfers();
  }

  protected loadWarehouses(): void {
    this.api.getWarehouses({ isActive: true, pageSize: 50 }).subscribe({
      next: (res) => {
        this.warehouses.set(res.items);
        if (res.items.length >= 2) {
          this.formSourceWhId = res.items[0].id;
          this.formDestWhId = res.items[1].id;
        }
      },
    });
  }

  protected loadTransfers(): void {
    this.loading.set(true);
    this.api
      .getStockTransfers({
        status: this.filterStatus || undefined,
        page: this.currentPage,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (res) => {
          this.transfers.set(res.items);
          this.totalTransfers = res.totalCount;
          this.loading.set(false);
        },
        error: (err) => {
          this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to load transfers.' });
          this.loading.set(false);
        },
      });
  }

  protected openCreate(): void {
    this.formLines = [{ productId: '', requestedQuantity: 10 }];
    this.isCreateOpen.set(true);
  }

  protected addLine(): void {
    this.formLines.push({ productId: '', requestedQuantity: 1 });
  }

  protected removeLine(idx: number): void {
    this.formLines.splice(idx, 1);
  }

  protected saveTransfer(): void {
    if (!this.formSourceWhId || !this.formDestWhId) {
      alert('Please select both source and destination warehouses.');
      return;
    }
    if (this.formSourceWhId === this.formDestWhId) {
      alert('Source and destination warehouses must be different.');
      return;
    }
    if (this.formLines.length === 0 || this.formLines.some((l) => !l.productId || l.requestedQuantity <= 0)) {
      alert('Please fill valid products and requested quantities.');
      return;
    }

    this.saving.set(true);
    const req: CreateStockTransferRequest = {
      sourceBranchId: '22222222-2222-2222-2222-222222222222',
      destinationBranchId: '22222222-2222-2222-2222-222222222222',
      sourceWarehouseId: this.formSourceWhId,
      destinationWarehouseId: this.formDestWhId,
      notes: this.formNotes || null,
      lines: this.formLines.map((l) => ({
        productId: l.productId,
        requestedQuantity: l.requestedQuantity,
      })),
    };

    this.api.createStockTransfer(req).subscribe({
      next: (trn) => {
        this.actionMessage.set({ type: 'ok', text: `Stock transfer request ${trn.transferNumber} created.` });
        this.saving.set(false);
        this.isCreateOpen.set(false);
        this.loadTransfers();
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to create transfer.' });
        this.saving.set(false);
      },
    });
  }

  protected viewDetails(trn: StockTransferDto): void {
    this.selectedTransfer.set(trn);
    this.isDetailOpen.set(true);
  }

  protected openDispatch(trn: StockTransferDto): void {
    this.selectedTransfer.set(trn);
    this.dispatchCarrier = 'Express Logistics';
    this.dispatchTracking = 'TRK-' + Math.floor(100000 + Math.random() * 900000);
    this.isDispatchOpen.set(true);
  }

  protected confirmDispatch(): void {
    const trn = this.selectedTransfer();
    if (!trn) return;

    this.saving.set(true);
    const req: DispatchStockTransferRequest = {
      carrierName: this.dispatchCarrier || null,
      trackingNumber: this.dispatchTracking || null,
    };

    this.api.dispatchStockTransfer(trn.id, req).subscribe({
      next: (dispatched) => {
        this.actionMessage.set({ type: 'ok', text: `Transfer ${dispatched.transferNumber} dispatched into transit.` });
        this.saving.set(false);
        this.isDispatchOpen.set(false);
        this.isDetailOpen.set(false);
        this.loadTransfers();
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Dispatch failed.' });
        this.saving.set(false);
      },
    });
  }

  protected openReceive(trn: StockTransferDto): void {
    this.selectedTransfer.set(trn);
    this.receivedQuantities = {};
    this.damagedQuantities = {};
    for (const l of trn.lines) {
      this.receivedQuantities[l.id] = l.dispatchedQuantity;
      this.damagedQuantities[l.id] = 0;
    }
    this.isReceiveOpen.set(true);
  }

  protected confirmReceive(): void {
    const trn = this.selectedTransfer();
    if (!trn) return;

    this.saving.set(true);
    const req: ReceiveStockTransferRequest = {
      lines: trn.lines.map((l) => ({
        lineId: l.id,
        receivedQuantity: Number(this.receivedQuantities[l.id] ?? l.dispatchedQuantity),
        damagedQuantity: Number(this.damagedQuantities[l.id] ?? 0),
      })),
    };

    this.api.receiveStockTransfer(trn.id, req).subscribe({
      next: (received) => {
        this.actionMessage.set({ type: 'ok', text: `Transfer ${received.transferNumber} received at destination.` });
        this.saving.set(false);
        this.isReceiveOpen.set(false);
        this.isDetailOpen.set(false);
        this.loadTransfers();
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Receive failed.' });
        this.saving.set(false);
      },
    });
  }

  protected closeDrawers(): void {
    this.isCreateOpen.set(false);
    this.isDetailOpen.set(false);
    this.isDispatchOpen.set(false);
    this.isReceiveOpen.set(false);
  }
}
