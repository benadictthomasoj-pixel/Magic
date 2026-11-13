import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  CreateStockAdjustmentRequest,
  StockAdjustmentDto,
  WarehouseDto,
} from '../../../core/contracts/inventory.models';
import { InventoryApiService } from '../../../core/services/inventory-api.service';

@Component({
  selector: 'magic-stock-adjustments',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './stock-adjustments.component.html',
  styleUrl: './stock-adjustments.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StockAdjustmentsComponent implements OnInit {
  private readonly api = inject(InventoryApiService);

  protected readonly adjustments = signal<readonly StockAdjustmentDto[]>([]);
  protected readonly warehouses = signal<readonly WarehouseDto[]>([]);
  protected readonly selectedAdjustment = signal<StockAdjustmentDto | null>(null);
  protected readonly isCreateOpen = signal<boolean>(false);
  protected readonly isDetailOpen = signal<boolean>(false);
  protected readonly loading = signal<boolean>(true);
  protected readonly saving = signal<boolean>(false);
  protected readonly actionMessage = signal<{ type: 'ok' | 'error'; text: string } | null>(null);

  protected filterWarehouseId = '';
  protected filterStatus = '';
  protected currentPage = 1;
  protected pageSize = 20;
  protected totalAdjustments = 0;

  // Create form model
  protected formWarehouseId = '';
  protected formReasonCode = 'Damage';
  protected formDescription = '';
  protected formLines: Array<{
    productId: string;
    adjustmentType: 'Increase' | 'Decrease';
    quantity: number;
    reasonCode: string;
    notes: string;
  }> = [];

  ngOnInit(): void {
    this.loadWarehouses();
    this.loadAdjustments();
  }

  protected loadWarehouses(): void {
    this.api.getWarehouses({ isActive: true, pageSize: 50 }).subscribe({
      next: (res) => {
        this.warehouses.set(res.items);
        if (res.items.length > 0 && !this.formWarehouseId) {
          this.formWarehouseId = res.items[0].id;
        }
      },
    });
  }

  protected loadAdjustments(): void {
    this.loading.set(true);
    this.api
      .getStockAdjustments({
        warehouseId: this.filterWarehouseId || undefined,
        status: this.filterStatus || undefined,
        page: this.currentPage,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (res) => {
          this.adjustments.set(res.items);
          this.totalAdjustments = res.totalCount;
          this.loading.set(false);
        },
        error: (err) => {
          this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to load adjustments.' });
          this.loading.set(false);
        },
      });
  }

  protected openCreate(): void {
    this.formLines = [
      {
        productId: '',
        adjustmentType: 'Increase',
        quantity: 1,
        reasonCode: 'Found',
        notes: '',
      },
    ];
    this.isCreateOpen.set(true);
  }

  protected addLine(): void {
    this.formLines.push({
      productId: '',
      adjustmentType: 'Increase',
      quantity: 1,
      reasonCode: this.formReasonCode,
      notes: '',
    });
  }

  protected removeLine(idx: number): void {
    this.formLines.splice(idx, 1);
  }

  protected saveAdjustment(): void {
    if (!this.formWarehouseId) {
      alert('Please select a warehouse.');
      return;
    }
    if (this.formLines.length === 0 || this.formLines.some((l) => !l.productId || l.quantity <= 0)) {
      alert('Please specify valid product and quantity for all lines.');
      return;
    }

    this.saving.set(true);
    const req: CreateStockAdjustmentRequest = {
      warehouseId: this.formWarehouseId,
      branchId: '22222222-2222-2222-2222-222222222222',
      reasonCode: this.formReasonCode,
      description: this.formDescription || null,
      lines: this.formLines.map((l) => ({
        productId: l.productId,
        adjustmentType: l.adjustmentType,
        quantity: l.quantity,
        reasonCode: l.reasonCode,
        notes: l.notes || null,
      })),
    };

    this.api.createStockAdjustment(req).subscribe({
      next: (created) => {
        this.actionMessage.set({ type: 'ok', text: `Stock adjustment ${created.adjustmentNumber} recorded.` });
        this.saving.set(false);
        this.isCreateOpen.set(false);
        this.loadAdjustments();
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to create adjustment.' });
        this.saving.set(false);
      },
    });
  }

  protected viewDetails(adj: StockAdjustmentDto): void {
    this.selectedAdjustment.set(adj);
    this.isDetailOpen.set(true);
  }

  protected approveAdjustment(adj: StockAdjustmentDto): void {
    if (!confirm(`Approve stock adjustment ${adj.adjustmentNumber}? This will immediately update inventory balances.`)) return;

    this.api.approveStockAdjustment(adj.id).subscribe({
      next: () => {
        this.actionMessage.set({ type: 'ok', text: `Stock adjustment ${adj.adjustmentNumber} approved & stock balances updated.` });
        this.isDetailOpen.set(false);
        this.loadAdjustments();
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Approval failed.' });
      },
    });
  }

  protected closeDrawers(): void {
    this.isCreateOpen.set(false);
    this.isDetailOpen.set(false);
  }
}

