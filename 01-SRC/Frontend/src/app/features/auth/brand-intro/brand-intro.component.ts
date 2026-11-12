import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  input,
  output,
  signal,
} from '@angular/core';
import { IntroStateService } from '../../../core/services/intro-state.service';

/**
 * 5-Second Animated Logo Experience for MAGIC ERP.
 *
 * Sequence:
 * 0.0 - 1.8s: Dark M reveals (0.2s - 1.0s), followed by continuous diagonal gold wing sweep (0.8s - 1.6s) and light sweep (1.25s - 1.75s).
 * 1.8 - 3.55s: MAGIC wordmark written letter-by-letter in continuous fluid calligraphy motion (M -> A -> G -> I -> C).
 * 3.55 - 3.8s: Wordmark settles into complete position.
 * 3.85 - 4.45s: Subtle gold reflection glides across the emblem.
 * 3.8s+: Completed logo reveals action buttons or triggers completed event.
 */
@Component({
  selector: 'magic-brand-intro',
  standalone: true,
  template: `
    <div
      class="intro-backdrop"
      role="region"
      aria-label="MAGIC Brand Presentation"
    >
      @if (isMounted()) {
        <div class="logo-stage">
          <!-- 1. Dark M of Upper Emblem (Reveals 0.2s - 1.0s) -->
          <img
            src="/branding/magic-emblem-dark-tight.png"
            alt="MAGIC Emblem"
            class="emblem-dark"
          />

          <!-- 2. Gold Wing with Smooth Diagonal Sweep (Reveals 0.8s - 1.6s) -->
          <div class="gold-wing-container">
            <img
              src="/branding/magic-emblem-gold-tight.png"
              alt=""
              class="emblem-gold"
            />
            <!-- Subtle Golden Light Sweep (1.25s - 1.75s) -->
            <div class="gold-light-sweep" aria-hidden="true"></div>
          </div>

          <!-- 3. MAGIC Wordmark: Continuous Calligraphy Handwriting (1.80s - 3.55s) -->
          <div class="wordmark-group">
            <img src="/branding/magic-letter-m-tight.png" alt="M" class="letter-glyph letter-m" />
            <img src="/branding/magic-letter-a-tight.png" alt="A" class="letter-glyph letter-a" />
            <img src="/branding/magic-letter-g-tight.png" alt="G" class="letter-glyph letter-g" />
            <img src="/branding/magic-letter-i-tight.png" alt="I" class="letter-glyph letter-i" />
            <img src="/branding/magic-letter-c-tight.png" alt="C" class="letter-glyph letter-c" />
          </div>

          <!-- 4. Subtle Gold Reflection across Completed Emblem (3.85s - 4.45s) -->
          <div class="emblem-reflection" aria-hidden="true"></div>
        </div>
      }

      <!-- Elegant Action Controls (Fade in after animation completion at ~3.8s) -->
      @if (isComplete() && showActions()) {
        <div class="intro-actions" role="group" aria-label="Brand Actions">
          <button type="button" class="action-btn replay-btn" (click)="replay()">
            <svg class="btn-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
              <path
                fill-rule="evenodd"
                d="M4 2a1 1 0 011 1v2.101a7.002 7.002 0 0111.601 2.566 1 1 0 11-1.885.666A5.002 5.002 0 005.999 7H9a1 1 0 010 2H4a1 1 0 01-1-1V3a1 1 0 011-1zm.008 9.057a1 1 0 011.276.61A5.002 5.002 0 0014.001 13H11a1 1 0 110-2h5a1 1 0 011 1v5a1 1 0 11-2 0v-2.101a7.002 7.002 0 01-11.601-2.566 1 1 0 01.61-1.276z"
                clip-rule="evenodd"
              />
            </svg>
            <span>Replay Animation</span>
          </button>

          <button type="button" class="action-btn enter-btn" (click)="proceed()">
            <span>Sign In to ERP Workspace</span>
            <svg class="btn-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
              <path
                fill-rule="evenodd"
                d="M10.293 3.293a1 1 0 011.414 0l6 6a1 1 0 010 1.414l-6 6a1 1 0 01-1.414-1.414L14.586 11H3a1 1 0 110-2h11.586l-4.293-4.293a1 1 0 010-1.414z"
                clip-rule="evenodd"
              />
            </svg>
          </button>
        </div>
      }
    </div>
  `,
  styleUrl: './brand-intro.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BrandIntroComponent implements OnInit {
  private readonly introState = inject(IntroStateService);

  readonly showActions = input<boolean>(true);
  readonly autoProceed = input<boolean>(false);
  readonly completed = output<void>();

  protected readonly isMounted = signal(true);
  protected readonly isComplete = signal(false);

  ngOnInit(): void {
    this.scheduleCompletion();
  }

  protected replay(): void {
    this.isMounted.set(false);
    this.isComplete.set(false);
    setTimeout(() => {
      this.isMounted.set(true);
      this.scheduleCompletion();
    }, 30);
  }

  protected proceed(): void {
    this.introState.markCompleted();
    this.completed.emit();
  }

  private scheduleCompletion(): void {
    setTimeout(() => {
      this.isComplete.set(true);
      if (this.autoProceed()) {
        this.proceed();
      }
    }, 3800);
  }
}
