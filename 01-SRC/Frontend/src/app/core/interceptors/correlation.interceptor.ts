import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
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

  if (Object.keys(headersToSet).length === 0) {
    return next(req);
  }

  return next(req.clone({ setHeaders: headersToSet }));
};

