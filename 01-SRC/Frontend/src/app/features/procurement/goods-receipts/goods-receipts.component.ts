import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { SupplierSummaryDto } from '../../../core/contracts/commercial.models';
import {
  CreateGoodsReceiptRequest,
  GoodsReceiptNoteDto,
  PurchaseOrderDto,
  WarehouseDto,
} from '../../../core/contracts/inventory.models';
import { CommercialApiService } from '../../../core/services/commercial-api.service';
import { InventoryApiService } from '../../../core/services/inventory-api.service';

@Component({
  selector: 'magic-goods-receipts',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './goods-receipts.component.html',
  styleUrl: './goods-receipts.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GoodsReceiptsComponent implements OnInit {
  private readonly inventoryApi = inject(InventoryApiService);
  private readonly commercialApi = inject(CommercialApiService);

  protected readonly receipts = signal<readonly GoodsReceiptNoteDto[]>([]);
  protected readonly warehouses = signal<readonly WarehouseDto[]>([]);
  protected readonly suppliers = signal<readonly SupplierSummaryDto[]>([]);
  protected readonly approvedPOs = signal<readonly PurchaseOrderDto[]>([]);
  protected readonly selectedReceipt = signal<GoodsReceiptNoteDto | null>(null);
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
  protected totalReceipts = 0;

  // Form model
  protected formWarehouseId = '';
  protected formSupplierId = '';
  protected formPurchaseOrderId = '';
  protected formInvoiceNumber = '';
  protected formInvoiceDate = new Date().toISOString().substring(0, 10);
  protected formNotes = '';
  protected formLines: Array<{
    purchaseOrderLineId?: string;
    productId: string;
    receivedQuantity: number;
    acceptedQuantity: number;
    rejectedQuantity: number;
    unitPriceRupees: number;
    gstRatePercentage: number;
    batchNumber: string;
    expiryDate: string;
  }> = [];

  ngOnInit(): void {
    this.loadWarehouses();
    this.loadSuppliers();
    this.loadReceipts();
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

  protected loadReceipts(): void {
    this.loading.set(true);
    this.inventoryApi
      .getGoodsReceipts({
        warehouseId: this.filterWarehouseId || undefined,
        supplierId: this.filterSupplierId || undefined,
        status: this.filterStatus || undefined,
        page: this.currentPage,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (res) => {
          this.receipts.set(res.items);
          this.totalReceipts = res.totalCount;
          this.loading.set(false);
        },
        error: (err) => {
          this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to load GRNs.' });
          this.loading.set(false);
        },
      });
  }

  protected openCreate(): void {
    this.inventoryApi.getPurchaseOrders({ status: 'Approved', pageSize: 50 }).subscribe({
      next: (res) => this.approvedPOs.set(res.items),
    });

    this.formLines = [
      {
        productId: '',
        receivedQuantity: 10,
        acceptedQuantity: 10,
        rejectedQuantity: 0,
        unitPriceRupees: 100,
        gstRatePercentage: 18,
        batchNumber: '',
        expiryDate: '',
      },
    ];
    this.isCreateOpen.set(true);
  }

  protected onPoSelected(poId: string): void {
    if (!poId) return;
    const po = this.approvedPOs().find((p) => p.id === poId);
    if (po) {
      this.formWarehouseId = po.warehouseId;
      this.formSupplierId = po.supplierId;
      this.formLines = po.lines.map((l) => ({
        purchaseOrderLineId: l.id,
        productId: l.productId,
        receivedQuantity: l.quantity - l.receivedQuantity,
        acceptedQuantity: l.quantity - l.receivedQuantity,
        rejectedQuantity: 0,
        unitPriceRupees: l.unitPricePaise / 100,
        gstRatePercentage: l.gstRatePercentage,
        batchNumber: '',
        expiryDate: '',
      }));
    }
  }

  protected addLine(): void {
    this.formLines.push({
      productId: '',
      receivedQuantity: 1,
      acceptedQuantity: 1,
      rejectedQuantity: 0,
      unitPriceRupees: 0,
      gstRatePercentage: 18,
      batchNumber: '',
      expiryDate: '',
    });
  }

  protected removeLine(idx: number): void {
    this.formLines.splice(idx, 1);
  }

  protected saveReceipt(): void {
    if (!this.formWarehouseId || !this.formSupplierId) {
      alert('Please select both warehouse and supplier.');
      return;
    }
    if (this.formLines.length === 0 || this.formLines.some((l) => !l.productId || l.acceptedQuantity < 0)) {
      alert('Please fill valid products and received quantities.');
      return;
    }

    this.saving.set(true);
    const req: CreateGoodsReceiptRequest = {
      branchId: '22222222-2222-2222-2222-222222222222',
      warehouseId: this.formWarehouseId,
      supplierId: this.formSupplierId,
      purchaseOrderId: this.formPurchaseOrderId || null,
      supplierInvoiceNumber: this.formInvoiceNumber || null,
      supplierInvoiceDate: this.formInvoiceDate || null,
      notes: this.formNotes || null,
      lines: this.formLines.map((l) => ({
        purchaseOrderLineId: l.purchaseOrderLineId || null,
        productId: l.productId,
        receivedQuantity: l.receivedQuantity,
        acceptedQuantity: l.acceptedQuantity,
        rejectedQuantity: l.rejectedQuantity || 0,
        unitPricePaise: Math.round(l.unitPriceRupees * 100),
        gstRatePercentage: l.gstRatePercentage,
        batchNumber: l.batchNumber || null,
        expiryDate: l.expiryDate || null,
      })),
    };

    this.inventoryApi.createGoodsReceipt(req).subscribe({
      next: (grn) => {
        this.actionMessage.set({ type: 'ok', text: `GRN ${grn.grnNumber} drafted.` });
        this.saving.set(false);
        this.isCreateOpen.set(false);
        this.loadReceipts();
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to create GRN.' });
        this.saving.set(false);
      },
    });
  }

  protected viewDetails(grn: GoodsReceiptNoteDto): void {
    this.selectedReceipt.set(grn);
    this.isDetailOpen.set(true);
  }

  protected confirmReceipt(grn: GoodsReceiptNoteDto): void {
    if (!confirm(`Confirm GRN ${grn.grnNumber}? This will atomically increase warehouse physical stock balances.`)) return;

    this.inventoryApi.confirmGoodsReceipt(grn.id).subscribe({
      next: () => {
        this.actionMessage.set({ type: 'ok', text: `GRN ${grn.grnNumber} confirmed and stock updated.` });
        this.isDetailOpen.set(false);
        this.loadReceipts();
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
