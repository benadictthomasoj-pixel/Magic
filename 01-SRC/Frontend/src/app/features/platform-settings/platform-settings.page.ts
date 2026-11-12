import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { ConnectivityMonitorService } from '../../core/services/connectivity-monitor.service';
import { ThemePreference, ThemeService } from '../../core/services/theme.service';
import { WorkspaceContextService } from '../../core/services/workspace-context.service';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'magic-platform-settings-page',
  standalone: true,
  imports: [ReactiveFormsModule, StatusBadgeComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="page">
      <header class="page-header">
        <h1>Workspace &amp; Display Foundation</h1>
        <p class="subtitle">
          Configure appearance tokens and optional Branch/Terminal diagnostic headers for API boundary testing.
        </p>
      </header>

      <section class="panel" aria-labelledby="theme-heading">
        <h2 id="theme-heading">Color Scheme Preference</h2>
        <p class="hint">
          Persists cleanly across browser sessions and WPF WebView2 instances using native CSS <code>color-scheme</code> and <code>light-dark()</code> design tokens.
        </p>

        <fieldset class="theme-group">
          <legend class="visually-hidden">Choose color scheme</legend>
          @for (option of themeOptions; track option.value) {
            <button
              type="button"
              class="theme-option"
              [attr.aria-pressed]="theme.preference() === option.value"
              (click)="theme.setPreference(option.value)"
            >
              {{ option.label }}
            </button>
          }
        </fieldset>
      </section>

      <section class="panel" aria-labelledby="context-heading">
        <div class="panel-head">
          <h2 id="context-heading">Branch &amp; Terminal Request Headers</h2>
          <magic-status-badge
            [label]="workspace.context().branchId ? 'Headers Active' : 'Headers Unbound'"
            [tone]="workspace.context().branchId ? 'ok' : 'neutral'"
          />
        </div>
        <p class="hint">
          Sends optional <code>X-Branch-Id</code> and <code>X-Terminal-Id</code> UUID headers on API calls so backend middleware logging and <code>/api/v1/system/context</code> can be verified end-to-end.
        </p>

        <form class="form" [formGroup]="form" (ngSubmit)="applyContextHeaders()">
          <div class="field">
            <label for="branch-id-input">Branch UUID (optional)</label>
            <input
              id="branch-id-input"
              type="text"
              formControlName="branchId"
              placeholder="e.g. 019a0000-0000-7000-8000-000000000001"
              aria-describedby="context-feedback"
            />
          </div>

          <div class="field">
            <label for="terminal-id-input">Terminal UUID (optional)</label>
            <input
              id="terminal-id-input"
              type="text"
              formControlName="terminalId"
              placeholder="e.g. 019a0000-0000-7000-8000-000000000002"
              aria-describedby="context-feedback"
            />
          </div>

          <div class="actions">
            <button type="submit" class="btn-primary">Apply Headers</button>
            <button type="button" class="btn-secondary" (click)="clearContextHeaders()">Clear Headers</button>
          </div>

          <p id="context-feedback" role="status" aria-live="polite" class="feedback">
            {{ feedback() }}
          </p>
        </form>
      </section>
    </div>
  `,
  styles: `
    .page {
      display: grid;
      gap: var(--magic-space-5);
      max-inline-size: 56rem;
    }

    .page-header h1 {
      font-size: 1.5rem;
      line-height: 1.25;
      color: var(--magic-text-primary);
    }

    .subtitle,
    .hint {
      margin-block-start: var(--magic-space-1);
      color: var(--magic-text-secondary);
    }

    .panel {
      padding: var(--magic-space-4);
      border-radius: var(--magic-radius-md);
      border: 1px solid var(--magic-border-subtle);
      background-color: var(--magic-surface-elevated);
      box-shadow: var(--magic-shadow-sm);
      display: grid;
      gap: var(--magic-space-3);
    }

    .panel-head {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      justify-content: space-between;
      gap: var(--magic-space-2);
    }

    h2 {
      font-size: 1.0625rem;
      color: var(--magic-text-primary);
    }

    .theme-group {
      border: 0;
      padding: 0;
      margin: 0;
      display: flex;
      flex-wrap: wrap;
      gap: var(--magic-space-2);
    }

    .theme-option,
    .btn-primary,
    .btn-secondary {
      min-block-size: 2.75rem;
      min-inline-size: 2.75rem;
      padding-inline: var(--magic-space-4);
      border-radius: var(--magic-radius-sm);
      border: 1px solid var(--magic-border-strong);
      background-color: var(--magic-surface-subtle);
      color: var(--magic-text-primary);
      font-weight: 600;
      cursor: pointer;
    }

    .theme-option[aria-pressed='true'],
    .btn-primary {
      border-color: var(--magic-accent);
      background-color: var(--magic-accent);
      color: var(--magic-accent-contrast);
    }

    .form {
      display: grid;
      gap: var(--magic-space-3);
    }

    .field {
      display: grid;
      gap: var(--magic-space-1);
    }

    label {
      font-weight: 600;
      color: var(--magic-text-primary);
    }

    input {
      min-block-size: 2.75rem;
      padding-inline: var(--magic-space-3);
      border-radius: var(--magic-radius-sm);
      border: 1px solid var(--magic-border-strong);
      background-color: var(--magic-surface-canvas);
      color: var(--magic-text-primary);
    }

    .actions {
      display: flex;
      flex-wrap: wrap;
      gap: var(--magic-space-2);
    }

    .feedback {
      min-block-size: 1.25rem;
      font-size: 0.875rem;
      color: var(--magic-text-secondary);
    }
  `,
})
export class PlatformSettingsPage {
  private readonly fb = inject(FormBuilder);
  protected readonly theme = inject(ThemeService);
  protected readonly workspace = inject(WorkspaceContextService);
  private readonly monitor = inject(ConnectivityMonitorService);

  protected readonly feedback = signal<string>('');
  protected readonly themeOptions: ReadonlyArray<{ value: ThemePreference; label: string }> = [
    { value: 'system', label: 'System Default' },
    { value: 'light', label: 'Light Mode' },
    { value: 'dark', label: 'Dark Mode' },
  ];

  protected readonly form = this.fb.group({
    branchId: [this.workspace.context().branchId ?? ''],
    terminalId: [this.workspace.context().terminalId ?? ''],
  });

  protected applyContextHeaders(): void {
    const raw = this.form.getRawValue();
    const ok = this.workspace.setContext(raw.branchId, raw.terminalId);
    if (!ok) {
      this.feedback.set('Enter valid UUID values (or leave blank) before applying.');
      return;
    }

    this.feedback.set('Diagnostic headers updated. Probing backend context...');
    this.monitor.refresh();
  }

  protected clearContextHeaders(): void {
    this.workspace.clearContext();
    this.form.reset({ branchId: '', terminalId: '' });
    this.feedback.set('Diagnostic headers cleared.');
    this.monitor.refresh();
  }
}

