import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { IntroStateService } from '../../../core/services/intro-state.service';
import { BrandIntroComponent } from './brand-intro.component';

describe('BrandIntroComponent', () => {
  let introState: IntroStateService;

  beforeEach(async () => {
    vi.useFakeTimers();
    await TestBed.configureTestingModule({
      imports: [BrandIntroComponent],
      providers: [provideRouter([])],
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
    const darkM = compiled.querySelector('.emblem-dark') as HTMLImageElement;
    const goldWing = compiled.querySelector('.emblem-gold') as HTMLImageElement;
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

    // Visual restrictions check: No skip button, no extra text, no cards
    expect(compiled.querySelector('.skip-button')).toBeNull();
    expect(compiled.querySelector('.intro-skip-btn')).toBeNull();
    expect(compiled.querySelector('.login-card-container')).toBeNull();
  });

  it('displays action controls after animation completes at 3.8s and supports replay', () => {
    const fixture = TestBed.createComponent(BrandIntroComponent);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.intro-actions')).toBeNull();

    // Fast-forward to 3800ms
    vi.advanceTimersByTime(3800);
    fixture.detectChanges();

    const actions = fixture.nativeElement.querySelector('.intro-actions');
    expect(actions).toBeTruthy();
    const replayBtn = fixture.nativeElement.querySelector('.replay-btn') as HTMLButtonElement;
    expect(replayBtn).toBeTruthy();

    // Test replay click
    replayBtn.click();
    fixture.detectChanges();

    // Fast-forward tick for remount
    vi.advanceTimersByTime(35);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.logo-stage')).toBeTruthy();
  });
});
