import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SupplierSummaryDto } from '../../../core/contracts/commercial.models';
import {
  CreatePurchaseOrderRequest,
  PurchaseOrderDto,
  WarehouseDto,
} from '../../../core/contracts/inventory.models';
import { CommercialApiService } from '../../../core/services/commercial-api.service';
import { InventoryApiService } from '../../../core/services/inventory-api.service';

@Component({
  selector: 'magic-purchase-orders',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './purchase-orders.component.html',
  styleUrl: './purchase-orders.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PurchaseOrdersComponent implements OnInit {
  private readonly inventoryApi = inject(InventoryApiService);
  private readonly commercialApi = inject(CommercialApiService);

  protected readonly purchaseOrders = signal<readonly PurchaseOrderDto[]>([]);
  protected readonly warehouses = signal<readonly WarehouseDto[]>([]);
  protected readonly suppliers = signal<readonly SupplierSummaryDto[]>([]);
  protected readonly selectedOrder = signal<PurchaseOrderDto | null>(null);
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
  protected totalOrders = 0;

  // New PO Form
  protected formWarehouseId = '';
  protected formSupplierId = '';
  protected formPaymentTerms = 'Net 30';
  protected formExpectedDate = '';
  protected formNotes = '';
  protected formLines: Array<{
    productId: string;
    quantity: number;
    unitPriceRupees: number;
    gstRatePercentage: number;
  }> = [];

  ngOnInit(): void {
    this.loadWarehouses();
    this.loadSuppliers();
    this.loadOrders();
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

  protected loadOrders(): void {
    this.loading.set(true);
    this.inventoryApi
      .getPurchaseOrders({
        warehouseId: this.filterWarehouseId || undefined,
        supplierId: this.filterSupplierId || undefined,
        status: this.filterStatus || undefined,
        page: this.currentPage,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (res) => {
          this.purchaseOrders.set(res.items);
          this.totalOrders = res.totalCount;
          this.loading.set(false);
        },
        error: (err) => {
          this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to load purchase orders.' });
          this.loading.set(false);
        },
      });
  }

  protected openCreate(): void {
    this.formLines = [
      {
        productId: '',
        quantity: 10,
        unitPriceRupees: 100,
        gstRatePercentage: 18,
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
    });
  }

  protected removeLine(idx: number): void {
    this.formLines.splice(idx, 1);
  }

  protected calculateSubtotal(): number {
    return this.formLines.reduce((acc, l) => acc + l.quantity * l.unitPriceRupees, 0);
  }

  protected calculateTotalTax(): number {
    return this.formLines.reduce((acc, l) => acc + (l.quantity * l.unitPriceRupees * l.gstRatePercentage) / 100, 0);
  }

  protected savePurchaseOrder(): void {
    if (!this.formWarehouseId || !this.formSupplierId) {
      alert('Please select both warehouse and supplier.');
      return;
    }
    if (this.formLines.length === 0 || this.formLines.some((l) => !l.productId || l.quantity <= 0)) {
      alert('Please fill valid product items with positive quantities.');
      return;
    }

    this.saving.set(true);
    const req: CreatePurchaseOrderRequest = {
      branchId: '22222222-2222-2222-2222-222222222222',
      warehouseId: this.formWarehouseId,
      supplierId: this.formSupplierId,
      paymentTerms: this.formPaymentTerms,
      expectedDeliveryDate: this.formExpectedDate || null,
      notes: this.formNotes || null,
      lines: this.formLines.map((l) => ({
        productId: l.productId,
        quantity: l.quantity,
        unitPricePaise: Math.round(l.unitPriceRupees * 100),
        gstRatePercentage: l.gstRatePercentage,
      })),
    };

    this.inventoryApi.createPurchaseOrder(req).subscribe({
      next: (po) => {
        this.actionMessage.set({ type: 'ok', text: `Purchase Order ${po.orderNumber} created.` });
        this.saving.set(false);
        this.isCreateOpen.set(false);
        this.loadOrders();
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to create PO.' });
        this.saving.set(false);
      },
    });
  }

  protected viewDetails(order: PurchaseOrderDto): void {
    this.selectedOrder.set(order);
    this.isDetailOpen.set(true);
  }

  protected approveOrder(order: PurchaseOrderDto): void {
    if (!confirm(`Approve purchase order ${order.orderNumber}?`)) return;

    this.inventoryApi.approvePurchaseOrder(order.id).subscribe({
      next: () => {
        this.actionMessage.set({ type: 'ok', text: `Purchase Order ${order.orderNumber} approved.` });
        this.isDetailOpen.set(false);
        this.loadOrders();
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Approval failed.' });
      },
    });
  }

  protected cancelOrder(order: PurchaseOrderDto): void {
    if (!confirm(`Cancel purchase order ${order.orderNumber}?`)) return;

    this.inventoryApi.cancelPurchaseOrder(order.id).subscribe({
      next: () => {
        this.actionMessage.set({ type: 'ok', text: `Purchase Order ${order.orderNumber} cancelled.` });
        this.isDetailOpen.set(false);
        this.loadOrders();
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Cancellation failed.' });
      },
    });
  }

  protected closeDrawers(): void {
    this.isCreateOpen.set(false);
    this.isDetailOpen.set(false);
  }
}
