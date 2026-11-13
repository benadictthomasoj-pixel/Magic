import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it } from 'vitest';
import { environment } from '../../../environments/environment';
import {
  CashierSessionDto,
  CreatePosSaleRequest,
  PosProductLookupDto,
  SalesInvoiceDto,
} from '../contracts/sales.models';
import { SalesApiService } from './sales-api.service';

describe('SalesApiService', () => {
  let service: SalesApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      providers: [
        SalesApiService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });

    service = TestBed.inject(SalesApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should lookup products for POS terminal', () => {
    const mockProducts: PosProductLookupDto[] = [
      {
        id: 'p-1',
        sku: 'SKU-001',
        name: 'Item 1',
        barcode: '12345678',
        categoryName: 'Groceries',
        unitOfMeasure: 'KG',
        sellingPricePaise: 25000,
        gstRatePercentage: 5,
        quantityOnHand: 40,
        quantityAvailable: 40,
      },
    ];

    service.lookupProducts('wh-1', 'Item').subscribe((data) => {
      expect(data.length).toBe(1);
      expect(data[0].name).toBe('Item 1');
      expect(data[0].sellingPricePaise).toBe(25000);
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/v1/pos/lookup?warehouseId=wh-1&query=Item`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: mockProducts });
  });

  it('should execute checkout and return created sales invoice', () => {
    const checkoutReq: CreatePosSaleRequest = {
      warehouseId: 'wh-1',
      lines: [{ productId: 'p-1', quantity: 2, discountPercentage: 0, discountPaise: 0 }],
      payments: [{ paymentMethod: 'Cash', amountPaise: 50000 }],
    };

    const mockInvoice: Partial<SalesInvoiceDto> = {
      id: 'inv-1',
      invoiceNumber: 'INV-20261113-123456',
      status: 'Paid',
      grandTotalPaise: 50000,
      subtotalPaise: 45000,
      totalTaxPaise: 5000,
      lines: [],
      payments: [],
    };

    service.checkout(checkoutReq).subscribe((inv) => {
      expect(inv.invoiceNumber).toBe('INV-20261113-123456');
      expect(inv.status).toBe('Paid');
      expect(inv.grandTotalPaise).toBe(50000);
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/v1/pos/checkout`);
    expect(req.request.method).toBe('POST');
    req.flush({ success: true, data: mockInvoice });
  });

  it('should fetch active cashier session', () => {
    const mockSession: Partial<CashierSessionDto> = {
      id: 'sess-1',
      cashierUsername: 'cashier1',
      status: 'Open',
      openingCashPaise: 500000,
      expectedCashPaise: 500000,
    };

    service.getActiveSession().subscribe((session) => {
      expect(session).not.toBeNull();
      expect(session?.status).toBe('Open');
      expect(session?.openingCashPaise).toBe(500000);
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/v1/cashier/sessions/active`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: mockSession });
  });
});
