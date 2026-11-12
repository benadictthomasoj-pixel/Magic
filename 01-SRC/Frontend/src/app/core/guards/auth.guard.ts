import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthBoundaryService } from '../services/auth-boundary.service';

/**
 * Route activation guard for MAGIC ERP workspace views.
 * Requires authenticated session; redirects to /login with returnUrl if unauthenticated.
 */
export const authGuard: CanActivateFn = (route, state) => {
  const auth = inject(AuthBoundaryService);
  const router = inject(Router);

  if (auth.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login'], {
    queryParams: { returnUrl: state.url },
  });
};

/**
 * Permission guard verifying specific required claim before activating route.
 */
export const permissionGuard = (requiredPermission: string): CanActivateFn => {
  return () => {
    const auth = inject(AuthBoundaryService);
    const router = inject(Router);

    if (!auth.isAuthenticated()) {
      return router.createUrlTree(['/login']);
    }

    if (auth.hasPermission(requiredPermission)) {
      return true;
    }

    return router.createUrlTree(['/access-denied'], {
      queryParams: { permission: requiredPermission },
    });
  };
};
