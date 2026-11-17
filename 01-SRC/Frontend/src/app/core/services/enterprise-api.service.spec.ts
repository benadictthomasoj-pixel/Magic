import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { ApiResponse } from '../contracts/api.models';
import {
  EnterpriseApiService,
  ExecutiveDashboard,
  CompanyProfile,
  AdminUser,
  AutomationRule,
  SystemHealthDashboard,
} from './enterprise-api.service';

describe('EnterpriseApiService (DEF-INT-001 Verification)', () => {
  let service: EnterpriseApiService;
  let httpMock: HttpTestingController;
  const baseUrl = environment.apiBaseUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        EnterpriseApiService,
        provideHttpClient(),
        provideHttpClientTesting(),
      ],
    });
    service = TestBed.inject(EnterpriseApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should fetch executive dashboard using configured environment apiBaseUrl', () => {
    const mockDashboard: Partial<ExecutiveDashboard> = {
      totalSalesPaise: 10000000,
      netSalesPaise: 9500000,
      grossProfitStatus: 'Healthy',
      totalPurchasesPaise: 5000000,
      operatingExpensesPaise: 2000000,
      netProfitStatus: 'Profitable',
      cashPositionPaise: 1500000,
      bankBalancesPaise: 3500000,
      accountsReceivablePaise: 1200000,
      accountsPayablePaise: 800000,
      totalInventoryQuantity: 450,
      lowStockAlertsCount: 2,
      pendingPurchaseOrdersCount: 1,
      pendingCustomerOrdersCount: 3,
      outstandingSupplierInvoicesCount: 2,
      activeEmployeesCount: 15,
      lastMonthPayrollPaise: 1200000,
      branchPerformance: [],
      dailyTrends: [],
      generatedAtUtc: new Date().toISOString(),
    };

    const mockResponse: ApiResponse<ExecutiveDashboard> = {
      success: true,
      data: mockDashboard as ExecutiveDashboard,
      correlationId: 'test-corr-id',
      timestampUtc: new Date().toISOString(),
    };

    service.getExecutiveDashboard().subscribe((data) => {
      expect(data.totalSalesPaise).toBe(10000000);
      expect(data.grossProfitStatus).toBe('Healthy');
    });

    const req = httpMock.expectOne(`${baseUrl}/v1/reports/dashboard`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should fetch company profile for multi-branch module using apiBaseUrl', () => {
    const mockProfile: CompanyProfile = {
      id: '00000000-0000-0000-0000-000000000001',
      legalName: 'MAGIC ERP Global Pvt Ltd',
      tradeName: 'MAGIC ERP',
      address: 'Bangalore, India',
      currencyCode: 'INR',
      fiscalYearStartMonth: 4,
      isActive: true,
      createdAtUtc: new Date().toISOString(),
    };

    const mockResponse: ApiResponse<CompanyProfile> = {
      success: true,
      data: mockProfile,
      correlationId: 'test-corr-id',
      timestampUtc: new Date().toISOString(),
    };

    service.getCompanyProfile().subscribe((profile) => {
      expect(profile.legalName).toBe('MAGIC ERP Global Pvt Ltd');
    });

    const req = httpMock.expectOne(`${baseUrl}/v1/branches/company/profile`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should fetch admin users for enterprise administration using apiBaseUrl', () => {
    const mockUsers: AdminUser[] = [
      {
        id: 'user-1',
        companyId: 'comp-1',
        username: 'admin',
        email: 'admin@magicerp.com',
        displayName: 'System Administrator',
        roles: ['SuperAdmin'],
        isLockedOut: false,
        isActive: true,
        failedLoginAttempts: 0,
        assignedBranchIds: [],
        createdAtUtc: new Date().toISOString(),
      },
    ];

    const mockResponse: ApiResponse<AdminUser[]> = {
      success: true,
      data: mockUsers,
      correlationId: 'test-corr-id',
      timestampUtc: new Date().toISOString(),
    };

    service.getUsers().subscribe((users) => {
      expect(users.length).toBe(1);
      expect(users[0].username).toBe('admin');
    });

    const req = httpMock.expectOne(`${baseUrl}/v1/administration/users`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should fetch automation rules for workflow engine using apiBaseUrl', () => {
    const mockRules: AutomationRule[] = [
      {
        id: 'rule-1',
        companyId: 'comp-1',
        name: 'Auto Reorder Low Stock',
        triggerType: 'InventoryBelowThreshold',
        conditionJson: '{}',
        actionType: 'CreateDraftPurchaseOrder',
        actionPayloadJson: '{}',
        isActive: true,
        createdAtUtc: new Date().toISOString(),
        executionCount: 5,
      },
    ];

    const mockResponse: ApiResponse<AutomationRule[]> = {
      success: true,
      data: mockRules,
      correlationId: 'test-corr-id',
      timestampUtc: new Date().toISOString(),
    };

    service.getAutomationRules().subscribe((rules) => {
      expect(rules.length).toBe(1);
      expect(rules[0].name).toBe('Auto Reorder Low Stock');
    });

    const req = httpMock.expectOne(`${baseUrl}/v1/automation/rules`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('should fetch system health for platform management using apiBaseUrl', () => {
    const mockHealth: SystemHealthDashboard = {
      applicationStatus: 'Operational',
      apiHealth: 'Healthy',
      databaseConnectivity: 'Connected',
      databaseVersion: 'PostgreSQL 18.6',
      applicationVersion: '2.0.0-phaseG',
      environmentName: 'Development',
      pendingOutboxMessagesCount: 0,
      failedOutboxMessagesCount: 0,
      activeBackgroundJobsCount: 2,
      uptimeFormatted: '14 days 2 hours',
      recentErrors: [],
      evaluatedAtUtc: new Date().toISOString(),
    };

    const mockResponse: ApiResponse<SystemHealthDashboard> = {
      success: true,
      data: mockHealth,
      correlationId: 'test-corr-id',
      timestampUtc: new Date().toISOString(),
    };

    service.getSystemHealth().subscribe((health) => {
      expect(health.applicationStatus).toBe('Operational');
      expect(health.databaseVersion).toBe('PostgreSQL 18.6');
    });

    const req = httpMock.expectOne(`${baseUrl}/v1/system-management/health`);
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });
});
