import { CanActivateFn } from '@angular/router';

/**
 * Route activation guard for MAGIC ERP workspace.
 * Allows direct access to ERP workspace views since dedicated login card is not required.
 */
export const authGuard: CanActivateFn = () => {
  return true;
};
