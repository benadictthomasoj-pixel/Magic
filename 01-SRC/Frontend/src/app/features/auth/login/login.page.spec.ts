import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { IntroStateService } from '../../../core/services/intro-state.service';
import { LoginPage } from './login.page';

describe('LoginPage', () => {
  it('renders brand intro animation initially when intro is not completed', async () => {
    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    const introState = TestBed.inject(IntroStateService);
    introState.resetForSession();

    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('magic-brand-intro')).toBeTruthy();
  });

  it('renders enterprise sign in form once intro is completed', async () => {
    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    const introState = TestBed.inject(IntroStateService);
    introState.markCompleted();

    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('.login-card-container')).toBeTruthy();
    expect(element.querySelector('#username-input')).toBeTruthy();
    expect(element.querySelector('#password-input')).toBeTruthy();
    expect(element.querySelector('.submit-btn')).toBeTruthy();
  });

  it('toggles password input visibility', async () => {
    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    }).compileComponents();

    const introState = TestBed.inject(IntroStateService);
    introState.markCompleted();

    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const passwordInput = element.querySelector('#password-input') as HTMLInputElement;
    expect(passwordInput.type).toBe('password');

    const toggleBtn = element.querySelector('.password-toggle-btn') as HTMLButtonElement;
    toggleBtn.click();
    fixture.detectChanges();

    expect(passwordInput.type).toBe('text');
  });
});
