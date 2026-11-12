import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthBoundaryService } from '../services/auth-boundary.service';

/**
 * Route activation guard protecting internal ERP workspace views.
 * Redirects unauthenticated users cleanly to /login without displaying empty shells.
 */
export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthBoundaryService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login']);
};

