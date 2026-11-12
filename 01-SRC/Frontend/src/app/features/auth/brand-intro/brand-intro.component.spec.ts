import { TestBed } from '@angular/core/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { IntroStateService } from '../../../core/services/intro-state.service';
import { BrandIntroComponent } from './brand-intro.component';

describe('BrandIntroComponent', () => {
  let introState: IntroStateService;

  beforeEach(async () => {
    vi.useFakeTimers();
    await TestBed.configureTestingModule({
      imports: [BrandIntroComponent],
    }).compileComponents();

    introState = TestBed.inject(IntroStateService);
    introState.resetForSession();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('renders official SVG emblem components and calligraphy stroke masks', () => {
    const fixture = TestBed.createComponent(BrandIntroComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const svg = compiled.querySelector('.magic-intro-svg');
    const darkM = compiled.querySelector('.img-dark-m') as SVGImageElement;
    const goldWing = compiled.querySelector('.img-gold-wing') as SVGImageElement;
    const wordmark = compiled.querySelector('.img-wordmark') as SVGImageElement;

    expect(svg).toBeTruthy();
    expect(darkM).toBeTruthy();
    expect(darkM.getAttribute('href')).toBe('/branding/magic-emblem-dark.png');

    expect(goldWing).toBeTruthy();
    expect(goldWing.getAttribute('href')).toBe('/branding/magic-emblem-gold.png');

    expect(wordmark).toBeTruthy();
    expect(wordmark.getAttribute('href')).toBe('/branding/magic-wordmark.png');

    // Calligraphy pen strokes
    expect(compiled.querySelector('.stroke-m')).toBeTruthy();
    expect(compiled.querySelector('.stroke-a-apex')).toBeTruthy();
    expect(compiled.querySelector('.stroke-a-bar')).toBeTruthy();
    expect(compiled.querySelector('.stroke-g')).toBeTruthy();
    expect(compiled.querySelector('.stroke-i')).toBeTruthy();
    expect(compiled.querySelector('.stroke-c')).toBeTruthy();

    // Visual restrictions check: No skip button, no extra text, no cards
    expect(compiled.querySelector('.skip-button')).toBeNull();
    expect(compiled.querySelector('.intro-skip-btn')).toBeNull();
  });

  it('completes the 5-second animation lifecycle and transitions to exit', () => {
    const fixture = TestBed.createComponent(BrandIntroComponent);
    const component = fixture.componentInstance;
    let completedEmitted = false;
    component.completed.subscribe(() => {
      completedEmitted = true;
    });

    fixture.detectChanges();
    expect(completedEmitted).toBe(false);

    // Fast-forward to 4500ms: exit fade starts
    vi.advanceTimersByTime(4500);
    fixture.detectChanges();
    const backdrop = fixture.nativeElement.querySelector('.intro-backdrop') as HTMLElement;
    expect(backdrop.classList.contains('phase-exit')).toBe(true);
    expect(completedEmitted).toBe(false);

    // Fast-forward to 5000ms: finishes exactly at 5.0 seconds
    vi.advanceTimersByTime(500);
    expect(completedEmitted).toBe(true);
    expect(introState.hasCompletedIntro()).toBe(true);
  });

  it('finishes immediately if intro was already completed in the session', () => {
    introState.markCompleted();

    const fixture = TestBed.createComponent(BrandIntroComponent);
    const component = fixture.componentInstance;
    let completedEmitted = false;
    component.completed.subscribe(() => {
      completedEmitted = true;
    });

    fixture.detectChanges();
    expect(completedEmitted).toBe(true);
  });
});
