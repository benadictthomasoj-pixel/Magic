import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { ConnectivityMonitorService } from './connectivity-monitor.service';

describe('ConnectivityMonitorService', () => {
  let service: ConnectivityMonitorService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ConnectivityMonitorService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('reports connected when /v1/system/status reports healthy database connectivity', () => {
    service.refresh();

    const statusReq = httpMock.expectOne(`${environment.apiBaseUrl}/v1/system/status`);
    statusReq.flush({
      success: true,
      correlationId: 'cid-1',
      timestampUtc: '2026-11-12T05:00:00Z',
      data: {
        status: 'Healthy',
        serviceName: 'MagicBackend',
        version: '1.0.0',
        environment: 'Development',
        deploymentMode: 'SingleComputer',
        databaseConnected: true,
        networkPolicy: 'StrictOnlineLan',
        authenticationStatus: 'Deferred (OPEN-003)',
        timestampUtc: '2026-11-12T05:00:00Z',
        components: [],
      },
    });

    const contextReq = httpMock.expectOne(`${environment.apiBaseUrl}/v1/system/context`);
    contextReq.flush({
      success: true,
      correlationId: 'cid-2',
      timestampUtc: '2026-11-12T05:00:00Z',
      data: {
        branchId: null,
        terminalId: null,
        authenticationConfigured: false,
        openDecisions: [],
      },
    });

    expect(service.connectivityState()).toBe('connected');
    expect(service.systemStatus()?.databaseConnected).toBe(true);
  });

  it('transitions to unreachable when backend fails in Strict Online LAN mode', () => {
    service.refresh();

    const statusReq = httpMock.expectOne(`${environment.apiBaseUrl}/v1/system/status`);
    statusReq.error(new ProgressEvent('error'));

    const contextReq = httpMock.expectOne(`${environment.apiBaseUrl}/v1/system/context`);
    contextReq.error(new ProgressEvent('error'));

    expect(service.connectivityState()).toBe('unreachable');
    expect(service.lastError()).toContain('Magic.Api');
  });
});

