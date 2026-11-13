import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  BackorderDto,
  OrderSummaryDto,
  SalesOrderDto,
} from '../../../core/contracts/operations.models';
import { OperationsApiService } from '../../../core/services/operations-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'magic-orders-management',
  standalone: true,
  imports: [CommonModule, FormsModule, StatusBadgeComponent],
  templateUrl: './orders-management.component.html',
  styleUrl: './orders-management.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OrdersManagementComponent implements OnInit {
  private readonly api = inject(OperationsApiService);

  protected readonly activeTab = signal<'orders' | 'backorders'>('orders');
  protected readonly summary = signal<OrderSummaryDto | null>(null);
  protected readonly orders = signal<readonly SalesOrderDto[]>([]);
  protected readonly backorders = signal<readonly BackorderDto[]>([]);
  protected readonly selectedOrder = signal<SalesOrderDto | null>(null);
  protected readonly loading = signal(true);

  protected readonly showNewOrderModal = signal(false);
  protected readonly showFulfillModal = signal(false);

  protected filterStatus = '';

  protected newOrder = {
    customerId: '11111111-1111-1111-1111-111111111111',
    orderDate: new Date().toISOString().split('T')[0],
    shippingAddress: '42 Commercial Street, Bangalore',
    notes: 'Standard Delivery',
    productId: '11111111-1111-1111-1111-111111111111',
    quantity: 1,
    unitPriceRupees: 1000,
    taxRatePercent: 18,
  };

  protected fulfillData = {
    warehouseId: '11111111-1111-1111-1111-111111111111',
    carrierName: 'BlueDart Express',
    trackingNumber: `TRK-${Math.floor(100000 + Math.random() * 900000)}`,
  };

  ngOnInit(): void {
    this.loadSummary();
    this.loadOrders();
    this.loadBackorders();
  }

  protected setTab(tab: 'orders' | 'backorders'): void {
    this.activeTab.set(tab);
    if (tab === 'backorders') {
      this.loadBackorders();
    }
  }

  protected loadSummary(): void {
    this.api.getOrderSummary().subscribe({
      next: (res) => this.summary.set(res),
      error: () => {},
    });
  }

  protected loadOrders(): void {
    this.loading.set(true);
    this.api.getOrders({ status: this.filterStatus || undefined }).subscribe({
      next: (res) => {
        this.orders.set(res.items);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadBackorders(): void {
    this.api.getBackorders().subscribe({
      next: (res) => this.backorders.set(res),
      error: () => {},
    });
  }

  protected selectOrder(order: SalesOrderDto): void {
    this.api.getOrderById(order.id).subscribe({
      next: (res) => this.selectedOrder.set(res),
      error: () => this.selectedOrder.set(order),
    });
  }

  protected submitOrder(): void {
    this.api
      .createOrder({
        customerId: this.newOrder.customerId,
        orderDate: this.newOrder.orderDate,
        shippingAddress: this.newOrder.shippingAddress,
        notes: this.newOrder.notes,
        lines: [
          {
            productId: this.newOrder.productId,
            quantity: this.newOrder.quantity,
            unitPricePaise: Math.round(this.newOrder.unitPriceRupees * 100),
            taxRatePercent: this.newOrder.taxRatePercent,
          },
        ],
      })
      .subscribe({
        next: (res) => {
          this.showNewOrderModal.set(false);
          this.loadOrders();
          this.loadSummary();
          this.selectOrder(res);
        },
      });
  }

  protected confirmOrder(id: string): void {
    this.api.confirmOrder(id).subscribe({
      next: (res) => {
        this.loadOrders();
        this.loadSummary();
        this.selectedOrder.set(res);
      },
    });
  }

  protected openFulfillModal(order: SalesOrderDto): void {
    this.selectedOrder.set(order);
    this.showFulfillModal.set(true);
  }

  protected fulfillOrder(): void {
    const order = this.selectedOrder();
    if (!order) return;

    const dispatchLines = order.lines.map((l) => ({
      salesOrderLineId: l.id,
      quantityDispatched: l.quantity - l.fulfilledQuantity,
    }));

    this.api
      .fulfillOrder(order.id, {
        warehouseId: this.fulfillData.warehouseId,
        carrierName: this.fulfillData.carrierName,
        trackingNumber: this.fulfillData.trackingNumber,
        lines: dispatchLines,
      })
      .subscribe({
        next: () => {
          this.showFulfillModal.set(false);
          this.loadOrders();
          this.loadSummary();
          this.selectOrder(order);
        },
      });
  }

  protected convertToInvoice(order: SalesOrderDto): void {
    this.api.convertOrderToInvoice(order.id, 'Cash').subscribe({
      next: (invId) => {
        alert(`Sales Invoice generated successfully: ${invId}`);
        this.loadOrders();
        this.loadSummary();
        this.selectOrder(order);
      },
    });
  }

  protected formatPaise(paise?: number): string {
    if (paise == null) return '0.00';
    return (paise / 100).toLocaleString('en-IN', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
  }
}

