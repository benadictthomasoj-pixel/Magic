import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  output,
  signal,
} from '@angular/core';
import { IntroStateService } from '../../../core/services/intro-state.service';

/**
 * Animated Logo Experience for MAGIC ERP.
 *
 * Sequence:
 * 0.0 - 0.2s: Calm background stage initialization.
 * 0.2 - 1.0s: Dark M reveals from baseline.
 * 0.8 - 1.6s: Gold wing reveals in continuous fluid sweep.
 * 1.8 - 3.55s: MAGIC wordmark written letter-by-letter in continuous fluid calligraphy motion (M -> A -> G -> I -> C).
 * 3.55 - 3.8s: Wordmark settles into complete position.
 * 3.8s+: Automatically initiates seamless fade-transition to the sign-in page.
 */
@Component({
  selector: 'magic-brand-intro',
  standalone: true,
  template: `
    <div
      class="intro-backdrop"
      [class.intro-exiting]="isExiting()"
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

          <!-- 2. Gold Wing with Smooth Continuous Sweep (Reveals 0.8s - 1.6s) -->
          <div class="gold-wing-container">
            <img
              src="/branding/magic-emblem-gold-tight.png"
              alt=""
              class="emblem-gold"
            />
          </div>

          <!-- 3. MAGIC Wordmark: Continuous Calligraphy Handwriting (1.80s - 3.55s) -->
          <div class="wordmark-group">
            <img src="/branding/magic-letter-m-tight.png" alt="M" class="letter-glyph letter-m" />
            <img src="/branding/magic-letter-a-tight.png" alt="A" class="letter-glyph letter-a" />
            <img src="/branding/magic-letter-g-tight.png" alt="G" class="letter-glyph letter-g" />
            <img src="/branding/magic-letter-i-tight.png" alt="I" class="letter-glyph letter-i" />
            <img src="/branding/magic-letter-c-tight.png" alt="C" class="letter-glyph letter-c" />
          </div>
        </div>
      }
    </div>
  `,
  styleUrl: './brand-intro.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BrandIntroComponent implements OnInit {
  private readonly introState = inject(IntroStateService);

  readonly completed = output<void>();

  protected readonly isMounted = signal(true);
  protected readonly isExiting = signal(false);

  ngOnInit(): void {
    this.scheduleCompletion();
  }

  protected proceed(): void {
    if (this.isExiting()) {
      return;
    }
    this.isExiting.set(true);
    setTimeout(() => {
      this.introState.markCompleted();
      this.completed.emit();
    }, 250);
  }

  private scheduleCompletion(): void {
    setTimeout(() => {
      this.proceed();
    }, 3800);
  }
}
