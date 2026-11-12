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

  it('renders official emblem components and MAGIC wordmark glyphs', () => {
    const fixture = TestBed.createComponent(BrandIntroComponent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const darkM = compiled.querySelector('.dark-m') as HTMLImageElement;
    const goldWing = compiled.querySelector('.gold-img') as HTMLImageElement;
    const letterM = compiled.querySelector('.letter-m') as HTMLImageElement;
    const letterA = compiled.querySelector('.letter-a') as HTMLImageElement;
    const letterG = compiled.querySelector('.letter-g') as HTMLImageElement;
    const letterI = compiled.querySelector('.letter-i') as HTMLImageElement;
    const letterC = compiled.querySelector('.letter-c') as HTMLImageElement;

    expect(darkM).toBeTruthy();
    expect(darkM.getAttribute('src')).toBe('/branding/magic-emblem-dark-tight.png');

    expect(goldWing).toBeTruthy();
    expect(goldWing.getAttribute('src')).toBe('/branding/magic-emblem-gold-tight.png');

    expect(letterM).toBeTruthy();
    expect(letterA).toBeTruthy();
    expect(letterG).toBeTruthy();
    expect(letterI).toBeTruthy();
    expect(letterC).toBeTruthy();

    // Visual restrictions check: No skip button, no extra text
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
