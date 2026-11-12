import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'magic-empty-state',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <section class="empty-panel" aria-live="polite">
      <h2>{{ heading() }}</h2>
      <p>{{ description() }}</p>
    </section>
  `,
  styles: `
    .empty-panel {
      padding: var(--magic-space-5);
      border-radius: var(--magic-radius-md);
      border: 1px dashed var(--magic-border-strong);
      background-color: var(--magic-surface-subtle);
      color: var(--magic-text-secondary);
      display: grid;
      gap: var(--magic-space-2);
    }

    h2 {
      font-size: 1rem;
      color: var(--magic-text-primary);
    }
  `,
})
export class EmptyStateComponent {
  readonly heading = input.required<string>();
  readonly description = input.required<string>();
}
