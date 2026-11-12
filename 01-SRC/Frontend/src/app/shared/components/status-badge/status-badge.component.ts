import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

export type StatusTone = 'ok' | 'warn' | 'error' | 'neutral';

@Component({
  selector: 'magic-status-badge',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <span class="badge" [attr.data-tone]="tone()">
      <span class="glyph" aria-hidden="true">{{ glyph() }}</span>
      <span>{{ label() }}</span>
    </span>
  `,
  styles: `
    .badge {
      display: inline-flex;
      align-items: center;
      gap: 0.375rem;
      padding-block: 0.25rem;
      padding-inline: 0.625rem;
      border-radius: 999px;
      border: 1px solid var(--magic-status-neutral-border);
      background-color: var(--magic-status-neutral-bg);
      color: var(--magic-status-neutral-fg);
      font-size: 0.8125rem;
      font-weight: 600;
      line-height: 1.25;
      white-space: nowrap;
    }

    .badge[data-tone='ok'] {
      border-color: var(--magic-status-ok-border);
      background-color: var(--magic-status-ok-bg);
      color: var(--magic-status-ok-fg);
    }

    .badge[data-tone='warn'] {
      border-color: var(--magic-status-warn-border);
      background-color: var(--magic-status-warn-bg);
      color: var(--magic-status-warn-fg);
    }

    .badge[data-tone='error'] {
      border-color: var(--magic-status-error-border);
      background-color: var(--magic-status-error-bg);
      color: var(--magic-status-error-fg);
    }

    .glyph {
      font-family: var(--magic-font-mono);
      font-size: 0.75rem;
    }
  `,
})
export class StatusBadgeComponent {
  readonly label = input.required<string>();
  readonly tone = input<StatusTone>('neutral');

  protected readonly glyph = computed(() => {
    switch (this.tone()) {
      case 'ok':
        return '[OK]';
      case 'warn':
        return '[!]';
      case 'error':
        return '[X]';
      default:
        return '[•]';
    }
  });
}

