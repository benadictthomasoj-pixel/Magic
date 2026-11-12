import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { RouterLink } from '@angular/router';
import { IntroStateService } from '../../../core/services/intro-state.service';

/**
 * 5-Second Animated Logo Experience for MAGIC ERP.
 *
 * Sequence:
 * 0.0 - 1.8s: Dark M reveals, followed by smooth non-flickering diagonal gold wing sweep (0.8s - 1.6s).
 * 1.8 - 3.8s: MAGIC wordmark written by continuous calligraphy pen stroke mask (M -> A -> G -> I -> C).
 * 3.8 - 4.5s: Complete emblem and wordmark held together with subtle gold reflection.
 * 4.5s+: Completed logo remains displayed on screen with replay and workspace actions.
 */
@Component({
  selector: 'magic-brand-intro',
  standalone: true,
  imports: [RouterLink],
  template: `
    <div
      class="intro-backdrop"
      role="region"
      aria-label="MAGIC Brand Presentation"
    >
      @if (isMounted()) {
        <div class="logo-viewport">
          <svg
            viewBox="0 0 1024 512"
            class="magic-intro-svg"
            xmlns="http://www.w3.org/2000/svg"
            xml:space="preserve"
          >
            <defs>
              <!-- 1. Dark M Mask: reveals smoothly from baseline (0.2s - 1.0s) -->
              <mask id="dark-m-mask" maskUnits="userSpaceOnUse">
                <rect
                  x="250"
                  y="100"
                  width="510"
                  height="270"
                  fill="white"
                  class="mask-rect-dark"
                />
              </mask>

              <!-- 2. Gold Wing Stroke Mask: continuous diagonal sweep (0.8s - 1.6s) -->
              <mask id="gold-wing-mask" maskUnits="userSpaceOnUse">
                <path
                  d="M 460 405 L 795 10"
                  stroke="white"
                  stroke-width="280"
                  stroke-linecap="round"
                  class="mask-path-gold-wing"
                />
              </mask>

              <!-- 3. Calligraphy Pen Stroke Masks for Wordmark (1.8s - 3.5s) -->
              <mask id="wordmark-mask" maskUnits="userSpaceOnUse">
                <!-- M: 1.80s - 2.26s -->
                <path
                  d="M 132 496 L 132 385 L 210 480 L 290 385 L 290 496"
                  stroke="white"
                  stroke-width="52"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  fill="none"
                  class="pen-stroke stroke-m"
                />
                <!-- A Apex: 2.26s - 2.54s -->
                <path
                  d="M 336 496 L 408 380 L 480 496"
                  stroke="white"
                  stroke-width="56"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  fill="none"
                  class="pen-stroke stroke-a-apex"
                />
                <!-- A Crossbar: 2.54s - 2.66s -->
                <path
                  d="M 352 452 L 464 452"
                  stroke="white"
                  stroke-width="48"
                  stroke-linecap="round"
                  fill="none"
                  class="pen-stroke stroke-a-bar"
                />
                <!-- G: 2.66s - 3.03s -->
                <path
                  d="M 640 398 L 595 382 L 545 400 L 522 437 L 545 474 L 595 492 L 642 476 L 642 440 L 590 440"
                  stroke="white"
                  stroke-width="56"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  fill="none"
                  class="pen-stroke stroke-g"
                />
                <!-- I: 3.03s - 3.15s -->
                <path
                  d="M 707 385 L 707 496"
                  stroke="white"
                  stroke-width="45"
                  stroke-linecap="round"
                  fill="none"
                  class="pen-stroke stroke-i"
                />
                <!-- C: 3.15s - 3.47s -->
                <path
                  d="M 895 398 L 850 382 L 800 400 L 778 437 L 800 474 L 850 492 L 895 476"
                  stroke="white"
                  stroke-width="56"
                  stroke-linecap="round"
                  stroke-linejoin="round"
                  fill="none"
                  class="pen-stroke stroke-c"
                />
              </mask>

              <!-- Gradient for Gold Accent Light Sweep -->
              <linearGradient id="gold-sweep-grad" x1="0%" y1="100%" x2="100%" y2="0%">
                <stop offset="0%" stop-color="#ffffff" stop-opacity="0" />
                <stop offset="45%" stop-color="#ffffff" stop-opacity="0.6" />
                <stop offset="55%" stop-color="#ffd56b" stop-opacity="0.8" />
                <stop offset="100%" stop-color="#ffffff" stop-opacity="0" />
              </linearGradient>

              <!-- Gradient for Complete Emblem Reflection (3.85s - 4.45s) -->
              <linearGradient id="emblem-refl-grad" x1="0%" y1="0%" x2="100%" y2="0%">
                <stop offset="0%" stop-color="#ffffff" stop-opacity="0" />
                <stop offset="45%" stop-color="#ffffff" stop-opacity="0.45" />
                <stop offset="55%" stop-color="#e7aa1b" stop-opacity="0.5" />
                <stop offset="100%" stop-color="#ffffff" stop-opacity="0" />
              </linearGradient>
            </defs>

            <!-- Layer 1: Dark M of Upper Emblem -->
            <image
              href="/branding/magic-emblem-dark.png"
              x="0"
              y="0"
              width="1024"
              height="512"
              mask="url(#dark-m-mask)"
              class="svg-layer img-dark-m"
            />

            <!-- Layer 2: Gold Wing of Upper Emblem (Stable vector stroke unmask) -->
            <image
              href="/branding/magic-emblem-gold.png"
              x="0"
              y="0"
              width="1024"
              height="512"
              mask="url(#gold-wing-mask)"
              class="svg-layer img-gold-wing"
            />

            <!-- Layer 3: Gold Light Sweep across the Gold Wing (1.25s - 1.75s) -->
            <path
              d="M 460 405 L 795 10"
              stroke="url(#gold-sweep-grad)"
              stroke-width="260"
              stroke-linecap="round"
              class="svg-layer gold-accent-sweep"
              mask="url(#gold-wing-mask)"
            />

            <!-- Layer 4: MAGIC Wordmark (Calligraphy Pen Stroke Unmask) -->
            <image
              href="/branding/magic-wordmark.png"
              x="0"
              y="0"
              width="1024"
              height="512"
              mask="url(#wordmark-mask)"
              class="svg-layer img-wordmark"
            />

            <!-- Layer 5: Subtle Reflection across Complete Emblem (3.85s - 4.45s) -->
            <rect
              x="260"
              y="25"
              width="520"
              height="355"
              fill="url(#emblem-refl-grad)"
              class="svg-layer emblem-subtle-reflection"
            />
          </svg>
        </div>
      }

      <!-- Elegant Action Controls (Fade in after animation completion at ~3.8s) -->
      @if (isComplete()) {
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

          <a routerLink="/system/status" class="action-btn enter-btn">
            <span>Enter ERP Workspace</span>
            <svg class="btn-icon" viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
              <path
                fill-rule="evenodd"
                d="M10.293 3.293a1 1 0 011.414 0l6 6a1 1 0 010 1.414l-6 6a1 1 0 01-1.414-1.414L14.586 11H3a1 1 0 110-2h11.586l-4.293-4.293a1 1 0 010-1.414z"
                clip-rule="evenodd"
              />
            </svg>
          </a>
        </div>
      }
    </div>
  `,
  styleUrl: './brand-intro.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BrandIntroComponent implements OnInit {
  private readonly introState = inject(IntroStateService);

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
    }, 20);
  }

  private scheduleCompletion(): void {
    setTimeout(() => {
      this.isComplete.set(true);
    }, 3800);
  }
}
