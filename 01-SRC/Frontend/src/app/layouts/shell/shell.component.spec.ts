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

  function flushSystemRequests(): void {
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
  }

  it('renders accessible landmark structure, skip link, and cycles theme preference', async () => {
    const fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();

    flushSystemRequests();

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

  it('renders top-centre module switcher and toggles navigation between Billing and Accounting', async () => {
    const fixture = TestBed.createComponent(ShellComponent);
    fixture.detectChanges();

    flushSystemRequests();

    await fixture.whenStable();
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;

    // Check switcher exists
    const switcher = el.querySelector('.module-switcher');
    expect(switcher).toBeTruthy();

    const billingTab = el.querySelector('#module-tab-billing') as HTMLButtonElement;
    const accountingTab = el.querySelector('#module-tab-accounting') as HTMLButtonElement;
    expect(billingTab).toBeTruthy();
    expect(accountingTab).toBeTruthy();

    // Default is Accounting
    expect(accountingTab.classList.contains('active')).toBe(true);
    expect(el.querySelector('.workspace-pill')?.textContent).toContain('Accounting Workspace');
    expect(el.textContent).toContain('Chart of Accounts');
    expect(el.textContent).not.toContain('POS / New Bill');

    // Switch to Billing
    billingTab.click();
    fixture.detectChanges();

    expect(billingTab.classList.contains('active')).toBe(true);
    expect(accountingTab.classList.contains('active')).toBe(false);
    expect(el.querySelector('.workspace-pill')?.textContent).toContain('Billing Workspace');
    expect(el.textContent).toContain('POS / New Bill');
    expect(el.textContent).toContain('Cash Closing');
    expect(el.textContent).not.toContain('Chart of Accounts');

    // Switch back to Accounting
    accountingTab.click();
    fixture.detectChanges();

    expect(accountingTab.classList.contains('active')).toBe(true);
    expect(billingTab.classList.contains('active')).toBe(false);
    expect(el.querySelector('.workspace-pill')?.textContent).toContain('Accounting Workspace');
    expect(el.textContent).toContain('Chart of Accounts');
    expect(el.textContent).not.toContain('POS / New Bill');
  });
});
