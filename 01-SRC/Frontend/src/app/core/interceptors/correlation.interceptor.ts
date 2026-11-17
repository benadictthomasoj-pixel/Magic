import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { from, throwError } from 'rxjs';
import { catchError, switchMap } from 'rxjs/operators';
import { MAGIC_HTTP_HEADERS } from '../contracts/api.models';
import { AuthBoundaryService } from '../services/auth-boundary.service';
import { WorkspaceContextService } from '../services/workspace-context.service';

function generateCorrelationId(): string {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  return `magic-${Date.now()}-${Math.random().toString(16).slice(2, 10)}`;
}

export const correlationInterceptor: HttpInterceptorFn = (req, next) => {
  const workspaceContext = inject(WorkspaceContextService);
  const auth = inject(AuthBoundaryService);
  const current = workspaceContext.context();

  const headersToSet: Record<string, string> = {};

  const token = auth.accessToken();
  if (token && !req.headers.has('Authorization')) {
    headersToSet['Authorization'] = `Bearer ${token}`;
  }

  if (!req.headers.has(MAGIC_HTTP_HEADERS.correlationId)) {
    headersToSet[MAGIC_HTTP_HEADERS.correlationId] = generateCorrelationId();
  }
  if (current.branchId && !req.headers.has(MAGIC_HTTP_HEADERS.branchId)) {
    headersToSet[MAGIC_HTTP_HEADERS.branchId] = current.branchId;
  }
  if (current.terminalId && !req.headers.has(MAGIC_HTTP_HEADERS.terminalId)) {
    headersToSet[MAGIC_HTTP_HEADERS.terminalId] = current.terminalId;
  }

  const outgoingReq = Object.keys(headersToSet).length === 0 ? req : req.clone({ setHeaders: headersToSet });

  return next(outgoingReq).pipe(
    catchError((err: unknown) => {
      if (err instanceof HttpErrorResponse && err.status === 401 && !req.url.includes('/auth/')) {
        return from(auth.refreshSession()).pipe(
          switchMap((refreshed) => {
            if (refreshed) {
              const newToken = auth.accessToken();
              const retriedReq = req.clone({
                setHeaders: {
                  ...headersToSet,
                  Authorization: `Bearer ${newToken}`,
                },
              });
              return next(retriedReq);
            }
            return throwError(() => err);
          })
        );
      }
      return throwError(() => err);
    })
  );
};
