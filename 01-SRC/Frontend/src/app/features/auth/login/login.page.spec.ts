import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { AuthBoundaryService } from '../../../core/services/auth-boundary.service';
import { IntroStateService } from '../../../core/services/intro-state.service';
import { LoginPage } from './login.page';

describe('LoginPage', () => {
  let router: Router;
  let authService: AuthBoundaryService;
  let introState: IntroStateService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [provideRouter([])],
    }).compileComponents();

    router = TestBed.inject(Router);
    authService = TestBed.inject(AuthBoundaryService);
    introState = TestBed.inject(IntroStateService);
    // Mark intro completed by default in unit test environment to test login form directly
    introState.markCompleted();
  });

  it('renders official brand logo and welcome message', () => {
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();
    const element = fixture.nativeElement as HTMLElement;

    const logo = element.querySelector('.login-logo') as HTMLImageElement;
    expect(logo).toBeTruthy();
    expect(logo.getAttribute('src')).toBe('/branding/magic-logo.png');

    const heading = element.querySelector('.welcome-heading');
    expect(heading?.textContent).toContain('Welcome back');
  });

  it('shows error banner when submitting empty credentials', async () => {
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();

    const form = fixture.nativeElement.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));
    fixture.detectChanges();

    const errorBanner = fixture.nativeElement.querySelector('.auth-error-banner');
    expect(errorBanner).toBeTruthy();
    expect(errorBanner.textContent).toContain('Please enter both your username and password.');
  });

  it('toggles password field between password and text type', () => {
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();

    const passwordInput = fixture.nativeElement.querySelector('#password-input') as HTMLInputElement;
    expect(passwordInput.type).toBe('password');

    const toggleBtn = fixture.nativeElement.querySelector('.password-toggle-btn') as HTMLButtonElement;
    toggleBtn.click();
    fixture.detectChanges();

    expect(passwordInput.type).toBe('text');
  });

  it('toggles forgot password recovery information card', () => {
    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();

    const forgotBtn = fixture.nativeElement.querySelector('.forgot-link-btn') as HTMLButtonElement;
    forgotBtn.click();
    fixture.detectChanges();

    const forgotCard = fixture.nativeElement.querySelector('.forgot-info-card');
    expect(forgotCard).toBeTruthy();
    expect(forgotCard.textContent).toContain('Credential Recovery Protocol');
  });

  it('authenticates valid credentials and navigates to workspace', async () => {
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const fixture = TestBed.createComponent(LoginPage);
    const component = fixture.componentInstance;
    fixture.detectChanges();

    // Set valid credentials
    (component as any).username = 'admin';
    (component as any).password = 'admin123';

    await (component as any).onSubmit();
    fixture.detectChanges();

    expect(authService.isAuthenticated()).toBe(true);
    expect(navigateSpy).toHaveBeenCalledWith(['/system/status']);
  });
});

