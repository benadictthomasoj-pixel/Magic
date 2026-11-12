import { Injectable, signal } from '@angular/core';

const UUID_REGEX = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export interface WorkspaceDiagnosticContext {
  readonly branchId: string | null;
  readonly terminalId: string | null;
}

/**
 * Holds optional operational Branch and Terminal UUID scope sent in request headers
 * (`X-Branch-Id`, `X-Terminal-Id`) for diagnostic boundary verification.
 */
@Injectable({ providedIn: 'root' })
export class WorkspaceContextService {
  private readonly contextSignal = signal<WorkspaceDiagnosticContext>({
    branchId: null,
    terminalId: null,
  });

  readonly context = this.contextSignal.asReadonly();

  setContext(branchId: string | null, terminalId: string | null): boolean {
    const cleanBranch = branchId?.trim() || null;
    const cleanTerminal = terminalId?.trim() || null;

    if ((cleanBranch && !UUID_REGEX.test(cleanBranch)) || (cleanTerminal && !UUID_REGEX.test(cleanTerminal))) {
      return false;
    }

    this.contextSignal.set({
      branchId: cleanBranch,
      terminalId: cleanTerminal,
    });
    return true;
  }

  clearContext(): void {
    this.contextSignal.set({ branchId: null, terminalId: null });
  }
}

