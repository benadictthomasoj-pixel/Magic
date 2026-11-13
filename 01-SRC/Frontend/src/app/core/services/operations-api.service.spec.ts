import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { OperationsApiService } from './operations-api.service';

describe('OperationsApiService', () => {
  let service: OperationsApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        OperationsApiService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(OperationsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch expense summary', () => {
    const mockSummary = {
      totalExpensesPaise: 500000,
      pendingApprovalPaise: 100000,
      paidExpensesPaise: 400000,
      pendingApprovalCount: 2,
      totalExpensesCount: 10,
    };

    service.getExpenseSummary().subscribe((res) => {
      expect(res.totalExpensesPaise).toBe(500000);
      expect(res.pendingApprovalCount).toBe(2);
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/v1/expenses/summary`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: mockSummary });
  });

  it('should fetch HR summary', () => {
    const mockSummary = {
      totalEmployees: 25,
      activeEmployees: 24,
      presentToday: 20,
      onLeaveToday: 2,
      pendingLeaves: 1,
    };

    service.getHrSummary().subscribe((res) => {
      expect(res.totalEmployees).toBe(25);
      expect(res.presentToday).toBe(20);
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/v1/hr/summary`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: mockSummary });
  });

  it('should fetch marketing summary', () => {
    const mockSummary = {
      totalActiveCampaigns: 4,
      totalActiveCoupons: 8,
      totalLoyaltyMembers: 120,
      totalRedemptionsPaise: 150000,
    };

    service.getMarketingSummary().subscribe((res) => {
      expect(res.totalActiveCampaigns).toBe(4);
      expect(res.totalLoyaltyMembers).toBe(120);
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/v1/marketing/summary`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: mockSummary });
  });

  it('should fetch order summary', () => {
    const mockSummary = {
      totalOrders: 50,
      pendingOrders: 5,
      confirmedOrders: 15,
      fulfilledOrders: 30,
      totalBackorders: 2,
    };

    service.getOrderSummary().subscribe((res) => {
      expect(res.totalOrders).toBe(50);
      expect(res.totalBackorders).toBe(2);
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/v1/orders/summary`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: mockSummary });
  });

  it('should fetch service summary', () => {
    const mockSummary = {
      openRequests: 12,
      inProgressRequests: 5,
      waitingForPartsRequests: 2,
      completedRequests: 8,
    };

    service.getServiceSummary().subscribe((res) => {
      expect(res.openRequests).toBe(12);
      expect(res.completedRequests).toBe(8);
    });

    const req = httpMock.expectOne(`${environment.apiBaseUrl}/v1/services/summary`);
    expect(req.request.method).toBe('GET');
    req.flush({ success: true, data: mockSummary });
  });
});

