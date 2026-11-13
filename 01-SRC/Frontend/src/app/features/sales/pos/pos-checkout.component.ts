import { CommonModule } from '@angular/common';
import { Component, HostListener, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CustomerSummaryDto } from '../../../core/contracts/commercial.models';
import { WarehouseDto } from '../../../core/contracts/inventory.models';
import {
  CashierSessionDto,
  CreatePosSaleRequest,
  PosCartItem,
  PosPaymentRequest,
  PosProductLookupDto,
  PosSaleLineRequest,
  SalesInvoiceDto,
} from '../../../core/contracts/sales.models';
import { CommercialApiService } from '../../../core/services/commercial-api.service';
import { InventoryApiService } from '../../../core/services/inventory-api.service';
import { SalesApiService } from '../../../core/services/sales-api.service';

@Component({
  selector: 'app-pos-checkout',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  templateUrl: './pos-checkout.component.html',
  styleUrls: ['./pos-checkout.component.scss'],
})
export class PosCheckoutComponent implements OnInit {
  private readonly salesApi = inject(SalesApiService);
  private readonly inventoryApi = inject(InventoryApiService);
  private readonly commercialApi = inject(CommercialApiService);

  warehouses: WarehouseDto[] = [];
  selectedWarehouseId = '';
  activeSession: CashierSessionDto | null = null;
  customers: CustomerSummaryDto[] = [];
  selectedCustomerId = '';

  searchQuery = '';
  catalogProducts: PosProductLookupDto[] = [];
  isLoadingCatalog = false;

  cart: PosCartItem[] = [];

  // Payment Modal
  showPaymentModal = false;
  paymentMethod: 'Cash' | 'Upi' | 'Card' | 'Split' = 'Cash';
  cashTenderPaise = 0;
  upiTenderPaise = 0;
  cardTenderPaise = 0;
  referenceNumber = '';
  saleNotes = '';
  isSubmitting = false;
  errorMessage = '';

  // Receipt Modal
  showReceiptModal = false;
  completedInvoice: SalesInvoiceDto | null = null;
  receiptSize: '58mm' | '80mm' = '80mm';

  ngOnInit(): void {
    this.loadInitialContext();
  }

  @HostListener('window:keydown', ['$event'])
  handleKeyboardShortcuts(event: KeyboardEvent): void {
    if (event.key === 'F12') {
      event.preventDefault();
      if (this.cart.length > 0 && !this.showPaymentModal) {
        this.openPaymentModal();
      }
    } else if (event.key === 'Escape') {
      if (this.showPaymentModal && !this.isSubmitting) {
        this.showPaymentModal = false;
      } else if (this.showReceiptModal) {
        this.showReceiptModal = false;
      }
    }
  }

  loadInitialContext(): void {
    this.inventoryApi.getWarehouses({ isActive: true }).subscribe({
      next: (res) => {
        this.warehouses = [...res.items];
        if (this.warehouses.length > 0) {
          this.selectedWarehouseId = this.warehouses[0].id;
          this.loadCatalog();
        }
      },
    });

    this.salesApi.getActiveSession().subscribe({
      next: (session) => {
        this.activeSession = session;
      },
    });

    this.commercialApi.getCustomers({ page: 1, pageSize: 100 }).subscribe({
      next: (res) => {
        this.customers = [...res.items];
      },
    });
  }

  onWarehouseChange(): void {
    this.cart = [];
    this.loadCatalog();
  }

  loadCatalog(): void {
    if (!this.selectedWarehouseId) return;
    this.isLoadingCatalog = true;
    this.salesApi.lookupProducts(this.selectedWarehouseId, this.searchQuery).subscribe({
      next: (products) => {
        this.catalogProducts = [...products];
        this.isLoadingCatalog = false;
      },
      error: () => {
        this.isLoadingCatalog = false;
      },
    });
  }

  onSearchInput(): void {
    this.loadCatalog();
  }

  onBarcodeScanned(barcode: string): void {
    const trimmed = barcode.trim();
    if (!trimmed) return;

    const match = this.catalogProducts.find(
      (p) => p.barcode?.toLowerCase() === trimmed.toLowerCase() || p.sku.toLowerCase() === trimmed.toLowerCase()
    );

    if (match) {
      this.addToCart(match);
      this.searchQuery = '';
      this.loadCatalog();
    }
  }

  addToCart(product: PosProductLookupDto): void {
    const existing = this.cart.find((i) => i.product.id === product.id);
    if (existing) {
      if (existing.quantity + 1 > product.quantityOnHand) {
        this.errorMessage = `Cannot add more '${product.name}'. Max available stock is ${product.quantityOnHand}.`;
        return;
      }
      existing.quantity += 1;
    } else {
      if (product.quantityOnHand < 1) {
        this.errorMessage = `Product '${product.name}' is out of stock in this warehouse.`;
        return;
      }
      this.cart.push({
        product,
        quantity: 1,
        discountPercentage: 0,
        discountPaise: 0,
      });
    }
    this.errorMessage = '';
  }

  updateQuantity(item: PosCartItem, delta: number): void {
    const newQty = item.quantity + delta;
    if (newQty <= 0) {
      this.removeFromCart(item);
    } else if (newQty > item.product.quantityOnHand) {
      this.errorMessage = `Cannot set quantity to ${newQty}. Max stock on hand is ${item.product.quantityOnHand}.`;
    } else {
      item.quantity = newQty;
      this.errorMessage = '';
    }
  }

  removeFromCart(item: PosCartItem): void {
    this.cart = this.cart.filter((i) => i !== item);
  }

  clearCart(): void {
    this.cart = [];
    this.errorMessage = '';
  }

  // Calculations
  get subtotalPaise(): number {
    return this.cart.reduce((sum, i) => sum + (i.unitPriceOverridePaise ?? i.product.sellingPricePaise) * i.quantity, 0);
  }

  get totalDiscountPaise(): number {
    return this.cart.reduce((sum, i) => sum + i.discountPaise, 0);
  }

  get totalTaxPaise(): number {
    return this.cart.reduce((sum, i) => {
      const price = i.unitPriceOverridePaise ?? i.product.sellingPricePaise;
      const taxable = Math.max(0, price * i.quantity - i.discountPaise);
      return sum + Math.round((taxable * i.product.gstRatePercentage) / 100);
    }, 0);
  }

  get grandTotalPaise(): number {
    return this.subtotalPaise - this.totalDiscountPaise + this.totalTaxPaise;
  }

  get totalItemsCount(): number {
    return this.cart.reduce((sum, i) => sum + i.quantity, 0);
  }

  // Payment
  openPaymentModal(): void {
    this.errorMessage = '';
    this.showPaymentModal = true;
    this.paymentMethod = 'Cash';
    this.cashTenderPaise = this.grandTotalPaise;
    this.upiTenderPaise = 0;
    this.cardTenderPaise = 0;
  }

  get totalTenderedPaise(): number {
    if (this.paymentMethod === 'Cash') return this.cashTenderPaise;
    if (this.paymentMethod === 'Upi') return this.upiTenderPaise || this.grandTotalPaise;
    if (this.paymentMethod === 'Card') return this.cardTenderPaise || this.grandTotalPaise;
    return this.cashTenderPaise + this.upiTenderPaise + this.cardTenderPaise;
  }

  get changeDuePaise(): number {
    return Math.max(0, this.totalTenderedPaise - this.grandTotalPaise);
  }

  get isPaymentValid(): boolean {
    return this.totalTenderedPaise >= this.grandTotalPaise;
  }

  completeCheckout(): void {
    if (!this.selectedWarehouseId) {
      this.errorMessage = 'Please select a valid store warehouse.';
      return;
    }
    if (this.cart.length === 0) {
      this.errorMessage = 'Cart is empty.';
      return;
    }
    if (!this.isPaymentValid) {
      this.errorMessage = 'Tendered payment is less than total bill amount.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';

    const lines: PosSaleLineRequest[] = this.cart.map((item) => ({
      productId: item.product.id,
      quantity: item.quantity,
      unitPriceOverridePaise: item.unitPriceOverridePaise,
      discountPercentage: item.discountPercentage,
      discountPaise: item.discountPaise,
      storageLocationId: item.product.defaultStorageLocationId,
    }));

    const payments: PosPaymentRequest[] = [];
    if (this.paymentMethod === 'Cash') {
      payments.push({
        paymentMethod: 'Cash',
        amountPaise: this.cashTenderPaise,
      });
    } else if (this.paymentMethod === 'Upi') {
      payments.push({
        paymentMethod: 'Upi',
        amountPaise: this.grandTotalPaise,
        referenceNumber: this.referenceNumber || 'UPI-APP',
      });
    } else if (this.paymentMethod === 'Card') {
      payments.push({
        paymentMethod: 'Card',
        amountPaise: this.grandTotalPaise,
        referenceNumber: this.referenceNumber || 'CARD-POS',
      });
    } else {
      if (this.cashTenderPaise > 0) payments.push({ paymentMethod: 'Cash', amountPaise: this.cashTenderPaise });
      if (this.upiTenderPaise > 0) payments.push({ paymentMethod: 'Upi', amountPaise: this.upiTenderPaise, referenceNumber: this.referenceNumber });
      if (this.cardTenderPaise > 0) payments.push({ paymentMethod: 'Card', amountPaise: this.cardTenderPaise, referenceNumber: this.referenceNumber });
    }

    const payload: CreatePosSaleRequest = {
      warehouseId: this.selectedWarehouseId,
      customerId: this.selectedCustomerId || null,
      cashierSessionId: this.activeSession?.id || null,
      notes: this.saleNotes || null,
      lines,
      payments,
    };

    this.salesApi.checkout(payload).subscribe({
      next: (invoice) => {
        this.isSubmitting = false;
        this.showPaymentModal = false;
        this.completedInvoice = invoice;
        this.showReceiptModal = true;
        this.cart = [];
        this.loadCatalog(); // refresh stock on hand
      },
      error: (err) => {
        this.isSubmitting = false;
        this.errorMessage = err.error?.message || err.message || 'Checkout failed.';
      },
    });
  }

  startNewSale(): void {
    this.showReceiptModal = false;
    this.completedInvoice = null;
    this.cart = [];
    this.errorMessage = '';
  }

  setCashTender(val: number): void {
    this.cashTenderPaise = Math.round(Number(val || 0) * 100);
  }

  setUpiTender(val: number): void {
    this.upiTenderPaise = Math.round(Number(val || 0) * 100);
  }

  setCardTender(val: number): void {
    this.cardTenderPaise = Math.round(Number(val || 0) * 100);
  }

  printReceipt(): void {
    window.print();
  }
}
