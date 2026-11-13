import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { PosProductLookupDto } from '../../../core/contracts/sales.models';
import { CommercialApiService } from '../../../core/services/commercial-api.service';
import { InventoryApiService } from '../../../core/services/inventory-api.service';
import { SalesApiService } from '../../../core/services/sales-api.service';
import { PosCheckoutComponent } from './pos-checkout.component';

describe('PosCheckoutComponent', () => {
  let component: PosCheckoutComponent;
  let fixture: ComponentFixture<PosCheckoutComponent>;

  const mockSalesApi = {
    lookupProducts: () => of([]),
    getActiveSession: () => of(null),
    checkout: () => of({}),
  };

  const mockInventoryApi = {
    getWarehouses: () =>
      of({
        items: [{ id: 'wh-1', name: 'Main Outlet', code: 'WH-MAIN' }],
        totalCount: 1,
      }),
  };

  const mockCommercialApi = {
    getCustomers: () => of({ items: [], totalCount: 0 }),
  };

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PosCheckoutComponent],
      providers: [
        provideRouter([]),
        { provide: SalesApiService, useValue: mockSalesApi },
        { provide: InventoryApiService, useValue: mockInventoryApi },
        { provide: CommercialApiService, useValue: mockCommercialApi },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(PosCheckoutComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should initialize with selected warehouse and empty cart', () => {
    expect(component.warehouses.length).toBe(1);
    expect(component.selectedWarehouseId).toBe('wh-1');
    expect(component.cart.length).toBe(0);
    expect(component.grandTotalPaise).toBe(0);
  });

  it('should add product to cart and calculate correct grand total in paise', () => {
    const product: PosProductLookupDto = {
      id: 'p-1',
      sku: 'SKU-APPLE',
      name: 'Fresh Apple',
      categoryName: 'Fruits',
      unitOfMeasure: 'KG',
      sellingPricePaise: 10000, // ₹100.00
      gstRatePercentage: 5,
      quantityOnHand: 20,
      quantityAvailable: 20,
    };

    component.addToCart(product);
    expect(component.cart.length).toBe(1);
    expect(component.cart[0].quantity).toBe(1);
    expect(component.subtotalPaise).toBe(10000);
    expect(component.totalTaxPaise).toBe(500); // 5% of 10000 = 500 paise
    expect(component.grandTotalPaise).toBe(10500); // 105.00 INR
  });

  it('should update cart quantity and prevent adding more than stock on hand', () => {
    const product: PosProductLookupDto = {
      id: 'p-2',
      sku: 'SKU-LIMITED',
      name: 'Limited Item',
      categoryName: 'Electronics',
      unitOfMeasure: 'PCS',
      sellingPricePaise: 50000,
      gstRatePercentage: 18,
      quantityOnHand: 2,
      quantityAvailable: 2,
    };

    component.addToCart(product);
    expect(component.cart[0].quantity).toBe(1);

    component.updateQuantity(component.cart[0], 1);
    expect(component.cart[0].quantity).toBe(2);

    // Try to exceed stock on hand
    component.updateQuantity(component.cart[0], 1);
    expect(component.cart[0].quantity).toBe(2);
    expect(component.errorMessage).toContain('Max stock on hand is 2');
  });

  it('should clear cart on demand', () => {
    const product: PosProductLookupDto = {
      id: 'p-3',
      sku: 'SKU-X',
      name: 'Item X',
      categoryName: 'General',
      unitOfMeasure: 'PCS',
      sellingPricePaise: 2000,
      gstRatePercentage: 0,
      quantityOnHand: 10,
      quantityAvailable: 10,
    };

    component.addToCart(product);
    expect(component.cart.length).toBe(1);

    component.clearCart();
    expect(component.cart.length).toBe(0);
    expect(component.grandTotalPaise).toBe(0);
  });

  it('should permit checkout with partial or zero tender when a credit customer is selected', () => {
    const product: PosProductLookupDto = {
      id: 'p-4',
      sku: 'SKU-CREDIT',
      name: 'Credit Item',
      categoryName: 'General',
      unitOfMeasure: 'PCS',
      sellingPricePaise: 100000,
      gstRatePercentage: 0,
      quantityOnHand: 10,
      quantityAvailable: 10,
    };

    component.addToCart(product);
    component.openPaymentModal();
    component.cashTenderPaise = 20000; // partial tender: ₹200 out of ₹1,000

    // Without customer selected -> payment is invalid
    component.selectedCustomerId = '';
    expect(component.isPaymentValid).toBe(false);

    // With customer selected -> credit sale permitted
    component.selectedCustomerId = 'cust-123';
    expect(component.isPaymentValid).toBe(true);
  });
});
