import {
  ChangeDetectionStrategy,
  Component,
  EventEmitter,
  OnInit,
  Output,
  inject,
  signal,
} from '@angular/core';
import { IntroStateService } from '../../../core/services/intro-state.service';

/**
 * 5-Second Animated Logo Introduction for MAGIC ERP.
 * Sequence:
 * 0.0 - 1.8s: Emblem entrance (Dark M, then Gold lightning element with golden light sweep)
 * 1.8 - 3.8s: MAGIC text writing animation (letter by letter, left to right)
 * 3.8 - 4.5s: Complete logo presentation with subtle gold reflection
 * 4.5 - 5.0s: Seamless fade transition into the login screen
 */
@Component({
  selector: 'magic-brand-intro',
  standalone: true,
  template: `
    <div
      class="intro-backdrop"
      [class.phase-exit]="isExiting()"
      role="region"
      aria-label="MAGIC Brand Introduction"
    >
      <div class="logo-stage">
        <!-- Dark M Element (Reveals 0.2s - 1.0s) -->
        <img
          src="/branding/magic-emblem-dark-tight.png"
          alt=""
          class="stage-layer dark-m"
          aria-hidden="true"
        />

        <!-- Gold Lightning Wing with Light Sweep (Reveals 0.8s - 1.6s, Sweep 1.2s - 1.8s) -->
        <div class="gold-wrapper">
          <img
            src="/branding/magic-emblem-gold-tight.png"
            alt=""
            class="gold-img"
            aria-hidden="true"
          />
          <div class="gold-sweep" aria-hidden="true"></div>
        </div>

        <!-- Upper Emblem Subtle Reflection (3.85s - 4.5s) -->
        <div class="emblem-reflection" aria-hidden="true"></div>

        <!-- MAGIC Wordmark Glyphs (Writing animation letter-by-letter 1.8s - 3.8s) -->
        <img
          src="/branding/magic-letter-m-tight.png"
          alt="M"
          class="stage-layer letter-glyph letter-m"
          aria-hidden="true"
        />
        <img
          src="/branding/magic-letter-a-tight.png"
          alt="A"
          class="stage-layer letter-glyph letter-a"
          aria-hidden="true"
        />
        <img
          src="/branding/magic-letter-g-tight.png"
          alt="G"
          class="stage-layer letter-glyph letter-g"
          aria-hidden="true"
        />
        <img
          src="/branding/magic-letter-i-tight.png"
          alt="I"
          class="stage-layer letter-glyph letter-i"
          aria-hidden="true"
        />
        <img
          src="/branding/magic-letter-c-tight.png"
          alt="C"
          class="stage-layer letter-glyph letter-c"
          aria-hidden="true"
        />
      </div>
    </div>
  `,
  styleUrl: './brand-intro.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BrandIntroComponent implements OnInit {
  private readonly introState = inject(IntroStateService);

  @Output() readonly completed = new EventEmitter<void>();

  protected readonly isExiting = signal(false);

  ngOnInit(): void {
    if (this.introState.hasCompletedIntro()) {
      this.finish();
      return;
    }

    // Stage 4 transition: start exit fade at 4500ms
    setTimeout(() => {
      this.isExiting.set(true);
    }, 4500);

    // Exact 5.0 seconds completion: finish at 5000ms
    setTimeout(() => {
      this.finish();
    }, 5000);
  }

  private finish(): void {
    this.introState.markCompleted();
    this.completed.emit();
  }
}
