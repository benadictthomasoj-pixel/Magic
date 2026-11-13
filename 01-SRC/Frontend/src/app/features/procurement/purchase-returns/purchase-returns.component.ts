import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SupplierSummaryDto } from '../../../core/contracts/commercial.models';
import {
  CreatePurchaseReturnRequest,
  PurchaseReturnDto,
  WarehouseDto,
} from '../../../core/contracts/inventory.models';
import { CommercialApiService } from '../../../core/services/commercial-api.service';
import { InventoryApiService } from '../../../core/services/inventory-api.service';

@Component({
  selector: 'magic-purchase-returns',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './purchase-returns.component.html',
  styleUrl: './purchase-returns.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PurchaseReturnsComponent implements OnInit {
  private readonly inventoryApi = inject(InventoryApiService);
  private readonly commercialApi = inject(CommercialApiService);

  protected readonly returns = signal<readonly PurchaseReturnDto[]>([]);
  protected readonly warehouses = signal<readonly WarehouseDto[]>([]);
  protected readonly suppliers = signal<readonly SupplierSummaryDto[]>([]);
  protected readonly selectedReturn = signal<PurchaseReturnDto | null>(null);
  protected readonly isCreateOpen = signal<boolean>(false);
  protected readonly isDetailOpen = signal<boolean>(false);
  protected readonly loading = signal<boolean>(true);
  protected readonly saving = signal<boolean>(false);
  protected readonly actionMessage = signal<{ type: 'ok' | 'error'; text: string } | null>(null);

  protected filterWarehouseId = '';
  protected filterSupplierId = '';
  protected filterStatus = '';
  protected currentPage = 1;
  protected pageSize = 20;
  protected totalReturns = 0;

  // Form model
  protected formWarehouseId = '';
  protected formSupplierId = '';
  protected formReasonCode = 'Defective';
  protected formNotes = '';
  protected formLines: Array<{
    productId: string;
    quantity: number;
    unitPriceRupees: number;
    gstRatePercentage: number;
    returnReason: string;
  }> = [];

  ngOnInit(): void {
    this.loadWarehouses();
    this.loadSuppliers();
    this.loadReturns();
  }

  protected loadWarehouses(): void {
    this.inventoryApi.getWarehouses({ isActive: true, pageSize: 50 }).subscribe({
      next: (res) => {
        this.warehouses.set(res.items);
        if (res.items.length > 0 && !this.formWarehouseId) {
          this.formWarehouseId = res.items[0].id;
        }
      },
    });
  }

  protected loadSuppliers(): void {
    this.commercialApi.getSuppliers({ isActive: true, pageSize: 50 }).subscribe({
      next: (res) => {
        this.suppliers.set(res.items);
        if (res.items.length > 0 && !this.formSupplierId) {
          this.formSupplierId = res.items[0].id;
        }
      },
    });
  }

  protected loadReturns(): void {
    this.loading.set(true);
    this.inventoryApi
      .getPurchaseReturns({
        warehouseId: this.filterWarehouseId || undefined,
        supplierId: this.filterSupplierId || undefined,
        status: this.filterStatus || undefined,
        page: this.currentPage,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (res) => {
          this.returns.set(res.items);
          this.totalReturns = res.totalCount;
          this.loading.set(false);
        },
        error: (err) => {
          this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to load purchase returns.' });
          this.loading.set(false);
        },
      });
  }

  protected openCreate(): void {
    this.formLines = [
      {
        productId: '',
        quantity: 1,
        unitPriceRupees: 100,
        gstRatePercentage: 18,
        returnReason: 'Damaged packaging',
      },
    ];
    this.isCreateOpen.set(true);
  }

  protected addLine(): void {
    this.formLines.push({
      productId: '',
      quantity: 1,
      unitPriceRupees: 0,
      gstRatePercentage: 18,
      returnReason: 'Quality defect',
    });
  }

  protected removeLine(idx: number): void {
    this.formLines.splice(idx, 1);
  }

  protected saveReturn(): void {
    if (!this.formWarehouseId || !this.formSupplierId) {
      alert('Please select both warehouse and supplier.');
      return;
    }
    if (this.formLines.length === 0 || this.formLines.some((l) => !l.productId || l.quantity <= 0)) {
      alert('Please fill valid products and return quantities.');
      return;
    }

    this.saving.set(true);
    const req: CreatePurchaseReturnRequest = {
      branchId: '22222222-2222-2222-2222-222222222222',
      warehouseId: this.formWarehouseId,
      supplierId: this.formSupplierId,
      reasonCode: this.formReasonCode,
      notes: this.formNotes || null,
      lines: this.formLines.map((l) => ({
        productId: l.productId,
        quantity: l.quantity,
        unitPricePaise: Math.round(l.unitPriceRupees * 100),
        gstRatePercentage: l.gstRatePercentage,
        returnReason: l.returnReason,
      })),
    };

    this.inventoryApi.createPurchaseReturn(req).subscribe({
      next: (ret) => {
        this.actionMessage.set({ type: 'ok', text: `Purchase return ${ret.returnNumber} recorded.` });
        this.saving.set(false);
        this.isCreateOpen.set(false);
        this.loadReturns();
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to create return.' });
        this.saving.set(false);
      },
    });
  }

  protected viewDetails(ret: PurchaseReturnDto): void {
    this.selectedReturn.set(ret);
    this.isDetailOpen.set(true);
  }

  protected confirmReturn(ret: PurchaseReturnDto): void {
    if (!confirm(`Confirm purchase return ${ret.returnNumber}? This will deduct inventory and dispatch back to vendor.`)) return;

    this.inventoryApi.confirmPurchaseReturn(ret.id).subscribe({
      next: () => {
        this.actionMessage.set({ type: 'ok', text: `Purchase return ${ret.returnNumber} confirmed.` });
        this.isDetailOpen.set(false);
        this.loadReturns();
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Confirmation failed.' });
      },
    });
  }

  protected closeDrawers(): void {
    this.isCreateOpen.set(false);
    this.isDetailOpen.set(false);
  }
}

