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
 * Cinematic startup brand introduction for MAGIC ERP.
 * Executes a 2.5s restrained, GPU-accelerated 4-stage reveal sequence:
 * 1. Gallery off-white background with radial ambient lighting.
 * 2. Smooth scale/fade emblem emergence with golden diagonal light sweep.
 * 3. Brand name and tagline presentation.
 * 4. Seamless fade-and-scale transition directly into the enterprise login screen.
 */
@Component({
  selector: 'magic-brand-intro',
  standalone: true,
  template: `
    <div
      class="intro-backdrop"
      [class.phase-revealing]="phase() >= 1"
      [class.phase-sweep]="phase() >= 2"
      [class.phase-settled]="phase() >= 3"
      [class.phase-exit]="phase() === 4"
      role="region"
      aria-label="MAGIC Brand Introduction"
    >
      <div class="ambient-glow" aria-hidden="true"></div>

      <div class="intro-content">
        <div class="logo-viewport">
          <img
            src="/branding/magic-logo.jpg"
            alt="MAGIC Universal Business Management ERP"
            class="brand-logo"
            width="320"
            height="213"
          />
          <div class="gold-sweep-overlay" aria-hidden="true"></div>
        </div>

        <div class="tagline-block" [class.visible]="phase() >= 3">
          <span class="tagline-text">Universal Business Management ERP</span>
          <span class="system-edition">Enterprise Platform Edition</span>
        </div>
      </div>

      <button
        type="button"
        class="skip-button"
        (click)="skipIntro()"
        aria-label="Skip brand introduction"
      >
        Skip
      </button>
    </div>
  `,
  styleUrl: './brand-intro.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: {
    '(document:keydown.escape)': 'skipIntro()',
    '(document:keydown.space)': 'skipIntro()',
  },
})
export class BrandIntroComponent implements OnInit {
  private readonly introState = inject(IntroStateService);

  @Output() readonly completed = new EventEmitter<void>();

  // Phases: 0 = Init, 1 = Background & Initial Emblem Fade, 2 = Gold Sweep, 3 = Tagline Hold, 4 = Exit Transition
  protected readonly phase = signal<number>(0);

  ngOnInit(): void {
    if (this.introState.hasCompletedIntro()) {
      this.finish();
      return;
    }

    this.runAnimationSequence();
  }

  private runAnimationSequence(): void {
    // Stage 1: Immediate background appearance with soft radial glow
    this.phase.set(1);

    // Stage 2 (t = 400ms): Gold light sweep across symbol
    setTimeout(() => {
      this.phase.set(2);
    }, 450);

    // Stage 3 (t = 1200ms): Wordmark & Tagline presence hold
    setTimeout(() => {
      this.phase.set(3);
    }, 1250);

    // Stage 4 (t = 2200ms): Smooth fade-and-scale transition to login
    setTimeout(() => {
      this.phase.set(4);
    }, 2250);

    // Final completion (t = 2650ms)
    setTimeout(() => {
      this.finish();
    }, 2650);
  }

  protected skipIntro(): void {
    this.finish();
  }

  private finish(): void {
    this.introState.markCompleted();
    this.completed.emit();
  }
}
