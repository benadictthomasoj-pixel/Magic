import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { MAGIC_HTTP_HEADERS } from '../contracts/api.models';
import { WorkspaceContextService } from '../services/workspace-context.service';
import { correlationInterceptor } from './correlation.interceptor';

describe('correlationInterceptor', () => {
  let http: HttpClient;
  let httpMock: HttpTestingController;
  let workspace: WorkspaceContextService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([correlationInterceptor])),
        provideHttpClientTesting(),
      ],
    });
    http = TestBed.inject(HttpClient);
    httpMock = TestBed.inject(HttpTestingController);
    workspace = TestBed.inject(WorkspaceContextService);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('attaches X-Correlation-Id and optional Branch/Terminal headers when configured', () => {
    const branchId = '019a0000-0000-7000-8000-000000000001';
    const terminalId = '019a0000-0000-7000-8000-000000000002';
    expect(workspace.setContext(branchId, terminalId)).toBe(true);

    http.get('/api/v1/system/status').subscribe();

    const req = httpMock.expectOne('/api/v1/system/status');
    expect(req.request.headers.get(MAGIC_HTTP_HEADERS.correlationId)).toBeTruthy();
    expect(req.request.headers.get(MAGIC_HTTP_HEADERS.branchId)).toBe(branchId);
    expect(req.request.headers.get(MAGIC_HTTP_HEADERS.terminalId)).toBe(terminalId);
    req.flush({});
  });

  it('rejects non-UUID context values', () => {
    expect(workspace.setContext('invalid-id', null)).toBe(false);
    expect(workspace.context().branchId).toBeNull();
  });
});
