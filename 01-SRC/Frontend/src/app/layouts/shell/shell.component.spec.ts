import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { environment } from '../../../environments/environment';
import { routes } from '../../app.routes';
import { ShellComponent } from './shell.component';

describe('ShellComponent', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ShellComponent],
      providers: [provideRouter(routes), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('renders accessible landmark structure, skip link, and cycles theme preference', async () => {
    const fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();

    const statusReq = httpMock.expectOne(`${environment.apiBaseUrl}/v1/system/status`);
    statusReq.flush({
      success: true,
      correlationId: 'cid-shell',
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
      correlationId: 'cid-ctx',
      timestampUtc: '2026-11-12T05:00:00Z',
      data: {
        branchId: null,
        terminalId: null,
        authenticationConfigured: false,
        openDecisions: [],
      },
    });

    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.querySelector('a.skip-link')?.getAttribute('href')).toBe('#main-content');
    expect(el.querySelector('main#main-content')).toBeTruthy();
    expect(el.querySelector('nav[aria-label="Primary"]')).toBeTruthy();

    const themeBtn = el.querySelector('button.theme-cycle') as HTMLButtonElement;
    expect(themeBtn.textContent).toContain('Theme: system');
    themeBtn.click();
    fixture.detectChanges();
    expect(themeBtn.textContent).toContain('Theme: light');
  });
});
