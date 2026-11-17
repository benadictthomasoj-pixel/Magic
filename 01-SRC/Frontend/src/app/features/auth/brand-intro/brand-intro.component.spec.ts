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

    // Visual restrictions check: No skip button, no extra text, no cards, no replay/action buttons
    expect(compiled.querySelector('.skip-button')).toBeNull();
    expect(compiled.querySelector('.intro-skip-btn')).toBeNull();
    expect(compiled.querySelector('.login-card-container')).toBeNull();
    expect(compiled.querySelector('.intro-actions')).toBeNull();
  });

  it('automatically completes animation at 3.8s and emits transition to sign in page', () => {
    const fixture = TestBed.createComponent(BrandIntroComponent);
    const completedSpy = vi.fn();
    fixture.componentInstance.completed.subscribe(completedSpy);
    fixture.detectChanges();

    expect(completedSpy).not.toHaveBeenCalled();
    expect(introState.hasCompletedIntro()).toBe(false);

    // Fast-forward to 3800ms (end of calligraphy and settling)
    vi.advanceTimersByTime(3800);
    fixture.detectChanges();

    // Fast-forward through smooth fade-out exit transition (250ms)
    vi.advanceTimersByTime(260);
    fixture.detectChanges();

    expect(completedSpy).toHaveBeenCalled();
    expect(introState.hasCompletedIntro()).toBe(true);
    expect(fixture.nativeElement.querySelector('.intro-actions')).toBeNull();
  });
});
