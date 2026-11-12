import { Injectable, signal } from '@angular/core';

const INTRO_SESSION_KEY = 'magic_intro_completed';

/**
 * Manages the single-run lifecycle of the cinematic startup brand introduction.
 * Ensures the intro plays once per session on application launch,
 * respects prefers-reduced-motion, and prevents replay on login validation errors or navigation.
 */
@Injectable({ providedIn: 'root' })
export class IntroStateService {
  private readonly prefersReducedMotion: boolean = this.detectReducedMotion();
  private readonly sessionCompleted: boolean = this.checkSessionCompleted();

  readonly hasCompletedIntro = signal<boolean>(this.prefersReducedMotion || this.sessionCompleted);

  markCompleted(): void {
    try {
      sessionStorage.setItem(INTRO_SESSION_KEY, 'true');
    } catch {
      // Ignore storage access exceptions in restricted environments
    }
    this.hasCompletedIntro.set(true);
  }

  resetForSession(): void {
    try {
      sessionStorage.removeItem(INTRO_SESSION_KEY);
    } catch {
      // Ignore
    }
    this.hasCompletedIntro.set(false);
  }

  private detectReducedMotion(): boolean {
    if (typeof window === 'undefined' || !window.matchMedia) {
      return false;
    }
    return window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  }

  private checkSessionCompleted(): boolean {
    if (typeof window === 'undefined' || !window.sessionStorage) {
      return false;
    }
    try {
      return sessionStorage.getItem(INTRO_SESSION_KEY) === 'true';
    } catch {
      return false;
    }
  }
}
