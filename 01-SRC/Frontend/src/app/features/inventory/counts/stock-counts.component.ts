import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  CreateStockCountRequest,
  RecordStockCountLinesRequest,
  StockCountDto,
  WarehouseDto,
} from '../../../core/contracts/inventory.models';
import { InventoryApiService } from '../../../core/services/inventory-api.service';

@Component({
  selector: 'magic-stock-counts',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './stock-counts.component.html',
  styleUrl: './stock-counts.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StockCountsComponent implements OnInit {
  private readonly api = inject(InventoryApiService);

  protected readonly countSessions = signal<readonly StockCountDto[]>([]);
  protected readonly warehouses = signal<readonly WarehouseDto[]>([]);
  protected readonly selectedSession = signal<StockCountDto | null>(null);
  protected readonly isCreateOpen = signal<boolean>(false);
  protected readonly isWorksheetOpen = signal<boolean>(false);
  protected readonly loading = signal<boolean>(true);
  protected readonly saving = signal<boolean>(false);
  protected readonly actionMessage = signal<{ type: 'ok' | 'error'; text: string } | null>(null);

  protected filterWarehouseId = '';
  protected filterStatus = '';
  protected currentPage = 1;
  protected pageSize = 20;
  protected totalCounts = 0;

  // New Count Session Model
  protected formWarehouseId = '';
  protected formCountType = 'Full';
  protected formScheduledDate = new Date().toISOString().substring(0, 10);
  protected formNotes = '';

  // Worksheet counted items: lineId -> countedQuantity
  protected countedValues: Record<string, number> = {};

  ngOnInit(): void {
    this.loadWarehouses();
    this.loadCounts();
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

  protected loadCounts(): void {
    this.loading.set(true);
    this.api
      .getStockCounts({
        warehouseId: this.filterWarehouseId || undefined,
        status: this.filterStatus || undefined,
        page: this.currentPage,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (res) => {
          this.countSessions.set(res.items);
          this.totalCounts = res.totalCount;
          this.loading.set(false);
        },
        error: (err) => {
          this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to load stock counts.' });
          this.loading.set(false);
        },
      });
  }

  protected openCreate(): void {
    this.isCreateOpen.set(true);
  }

  protected createCountSession(): void {
    if (!this.formWarehouseId) {
      alert('Please select a warehouse.');
      return;
    }

    this.saving.set(true);
    const req: CreateStockCountRequest = {
      warehouseId: this.formWarehouseId,
      branchId: '22222222-2222-2222-2222-222222222222',
      countType: this.formCountType,
      scheduledDate: this.formScheduledDate,
      notes: this.formNotes || null,
    };

    this.api.createStockCount(req).subscribe({
      next: (created) => {
        this.actionMessage.set({ type: 'ok', text: `Stock count audit session ${created.countNumber} initialized.` });
        this.saving.set(false);
        this.isCreateOpen.set(false);
        this.loadCounts();
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to create count session.' });
        this.saving.set(false);
      },
    });
  }

  protected openWorksheet(session: StockCountDto): void {
    this.selectedSession.set(session);
    this.countedValues = {};
    if (session.lines) {
      for (const line of session.lines) {
        this.countedValues[line.id] = line.countedQuantity ?? line.systemQuantity;
      }
    }
    this.isWorksheetOpen.set(true);
  }

  protected saveCountedLines(): void {
    const session = this.selectedSession();
    if (!session || !session.lines) return;

    this.saving.set(true);
    const req: RecordStockCountLinesRequest = {
      lines: session.lines.map((l) => ({
        lineId: l.id,
        countedQuantity: Number(this.countedValues[l.id] ?? l.systemQuantity),
      })),
    };

    this.api.recordStockCountLines(session.id, req).subscribe({
      next: (updated) => {
        this.selectedSession.set(updated);
        this.actionMessage.set({ type: 'ok', text: 'Counted physical quantities saved.' });
        this.saving.set(false);
        this.loadCounts();
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to record counts.' });
        this.saving.set(false);
      },
    });
  }

  protected reconcileCount(session: StockCountDto): void {
    if (!confirm(`Reconcile stock count ${session.countNumber}? This will adjust physical inventory to match counted quantities.`)) return;

    this.api.reconcileStockCount(session.id).subscribe({
      next: () => {
        this.actionMessage.set({ type: 'ok', text: `Stock count ${session.countNumber} reconciled successfully.` });
        this.isWorksheetOpen.set(false);
        this.loadCounts();
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Reconciliation failed.' });
      },
    });
  }

  protected closeDrawers(): void {
    this.isCreateOpen.set(false);
    this.isWorksheetOpen.set(false);
  }
}

