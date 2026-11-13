import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  PostOpeningStockRequest,
  StockBalanceDto,
  StockMovementDto,
  WarehouseDto,
} from '../../../core/contracts/inventory.models';
import { CommercialApiService } from '../../../core/services/commercial-api.service';
import { InventoryApiService } from '../../../core/services/inventory-api.service';

@Component({
  selector: 'magic-inventory-status',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './inventory-status.component.html',
  styleUrl: './inventory-status.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InventoryStatusComponent implements OnInit {
  private readonly inventoryApi = inject(InventoryApiService);
  private readonly commercialApi = inject(CommercialApiService);

  protected readonly activeTab = signal<'balances' | 'movements' | 'opening'>('balances');
  protected readonly balances = signal<readonly StockBalanceDto[]>([]);
  protected readonly movements = signal<readonly StockMovementDto[]>([]);
  protected readonly warehouses = signal<readonly WarehouseDto[]>([]);
  protected readonly loading = signal<boolean>(true);
  protected readonly actionMessage = signal<{ type: 'ok' | 'error'; text: string } | null>(null);

  // Filters
  protected filterWarehouseId = '';
  protected filterSearch = '';
  protected filterLowStock = false;
  protected currentPage = 1;
  protected pageSize = 25;
  protected totalItems = 0;

  // Opening Stock Entry Model
  protected openingWarehouseId = '';
  protected openingItems: Array<{
    productId: string;
    productName?: string;
    quantity: number;
    unitCostRupees: number;
    batchNumber: string;
    expiryDate: string;
  }> = [];

  ngOnInit(): void {
    this.loadWarehouses();
    this.loadBalances();
  }

  protected switchTab(tab: 'balances' | 'movements' | 'opening'): void {
    this.activeTab.set(tab);
    this.currentPage = 1;
    if (tab === 'balances') {
      this.loadBalances();
    } else if (tab === 'movements') {
      this.loadMovements();
    }
  }

  protected loadWarehouses(): void {
    this.inventoryApi.getWarehouses({ isActive: true, pageSize: 50 }).subscribe({
      next: (res) => {
        this.warehouses.set(res.items);
        if (res.items.length > 0 && !this.openingWarehouseId) {
          this.openingWarehouseId = res.items[0].id;
        }
      },
    });
  }

  protected loadBalances(): void {
    this.loading.set(true);
    this.inventoryApi
      .getStockBalances({
        warehouseId: this.filterWarehouseId || undefined,
        searchTerm: this.filterSearch.trim() || undefined,
        lowStockOnly: this.filterLowStock ? true : undefined,
        page: this.currentPage,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (res) => {
          this.balances.set(res.items);
          this.totalItems = res.totalCount;
          this.loading.set(false);
        },
        error: (err) => {
          this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to load stock balances.' });
          this.loading.set(false);
        },
      });
  }

  protected loadMovements(): void {
    this.loading.set(true);
    this.inventoryApi
      .getStockMovements({
        warehouseId: this.filterWarehouseId || undefined,
        page: this.currentPage,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (res) => {
          this.movements.set(res.items);
          this.totalItems = res.totalCount;
          this.loading.set(false);
        },
        error: (err) => {
          this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to load movements.' });
          this.loading.set(false);
        },
      });
  }

  protected addOpeningRow(): void {
    this.openingItems.push({
      productId: '',
      quantity: 1,
      unitCostRupees: 0,
      batchNumber: '',
      expiryDate: '',
    });
  }

  protected removeOpeningRow(index: number): void {
    this.openingItems.splice(index, 1);
  }

  protected submitOpeningStock(): void {
    if (!this.openingWarehouseId) {
      alert('Please select a destination warehouse.');
      return;
    }
    if (this.openingItems.length === 0) {
      alert('Please add at least one product item.');
      return;
    }

    const payload: PostOpeningStockRequest = {
      warehouseId: this.openingWarehouseId,
      branchId: '22222222-2222-2222-2222-222222222222',
      notes: 'Initial opening stock entry',
      items: this.openingItems.map((item) => ({
        productId: item.productId,
        quantity: item.quantity,
        unitCostPaise: Math.round(item.unitCostRupees * 100),
        batchNumber: item.batchNumber || null,
        expiryDate: item.expiryDate || null,
      })),
    };

    this.inventoryApi.postOpeningStock(payload).subscribe({
      next: () => {
        this.actionMessage.set({ type: 'ok', text: 'Opening stock posted successfully.' });
        this.openingItems = [];
        this.switchTab('balances');
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to post opening stock.' });
      },
    });
  }
}

