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
    vi.advanceTimersByTime(25);
    fixture.detectChanges();
    expect(fixture.nativeElement.querySelector('.magic-intro-svg')).toBeTruthy();
  });
});
