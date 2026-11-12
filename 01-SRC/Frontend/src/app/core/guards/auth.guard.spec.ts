import { TestBed } from '@angular/core/testing';
import { Router, UrlTree, provideRouter } from '@angular/router';
import { AuthBoundaryService } from '../services/auth-boundary.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let authService: AuthBoundaryService;
  let router: Router;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideRouter([])],
    });
    authService = TestBed.inject(AuthBoundaryService);
    router = TestBed.inject(Router);
  });

  it('redirects unauthenticated users to /login', () => {
    const result = TestBed.runInInjectionContext(() => authGuard({} as any, {} as any));
    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toBe('/login');
  });

  it('allows navigation when user is authenticated', async () => {
    await authService.login('admin', 'password123');
    const result = TestBed.runInInjectionContext(() => authGuard({} as any, {} as any));
    expect(result).toBe(true);
  });
});
