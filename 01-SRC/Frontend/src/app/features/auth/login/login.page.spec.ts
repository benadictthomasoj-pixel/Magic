import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { describe, expect, it } from 'vitest';
import { LoginPage } from './login.page';

describe('LoginPage', () => {
  it('renders brand intro animation without unnecessary login card', async () => {
    await TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [provideRouter([])],
    }).compileComponents();

    const fixture = TestBed.createComponent(LoginPage);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('magic-brand-intro')).toBeTruthy();

    // Verify login card and form are completely absent
    expect(element.querySelector('.login-card-container')).toBeNull();
    expect(element.querySelector('form')).toBeNull();
    expect(element.querySelector('input[type="password"]')).toBeNull();
  });
});
