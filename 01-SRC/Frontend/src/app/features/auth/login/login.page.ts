import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthBoundaryService } from '../../../core/services/auth-boundary.service';
import { IntroStateService } from '../../../core/services/intro-state.service';
import { BrandIntroComponent } from '../brand-intro/brand-intro.component';

@Component({
  selector: 'magic-login',
  standalone: true,
  imports: [FormsModule, BrandIntroComponent],
  templateUrl: './login.page.html',
  styleUrl: './login.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage implements OnInit {
  private readonly auth = inject(AuthBoundaryService);
  private readonly router = inject(Router);
  protected readonly introState = inject(IntroStateService);

  protected readonly isIntroPlaying = signal(false);
  protected readonly showPassword = signal(false);
  protected readonly isLoading = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly showForgotInfo = signal(false);

  protected username = '';
  protected password = '';
  protected rememberMe = true;

  ngOnInit(): void {
    if (!this.introState.hasCompletedIntro()) {
      this.isIntroPlaying.set(true);
    }

    // If already authenticated, redirect to workspace
    if (this.auth.isAuthenticated()) {
      this.router.navigate(['/system/status']);
    }
  }

  protected onIntroFinished(): void {
    this.isIntroPlaying.set(false);
  }

  protected togglePasswordVisibility(): void {
    this.showPassword.update((visible) => !visible);
  }

  protected toggleForgotInfo(): void {
    this.showForgotInfo.update((open) => !open);
  }

  protected async onSubmit(): Promise<void> {
    this.errorMessage.set(null);
    this.showForgotInfo.set(false);

    const trimmedUser = this.username.trim();
    if (!trimmedUser || !this.password) {
      this.errorMessage.set('Please enter both your username and password.');
      return;
    }

    this.isLoading.set(true);

    try {
      const result = await this.auth.login(trimmedUser, this.password);
      if (result.success) {
        await this.router.navigate(['/system/status']);
      } else {
        this.errorMessage.set(result.error ?? 'Authentication failed. Please verify credentials.');
      }
    } catch {
      this.errorMessage.set('Unable to complete sign-in. Please check your local network connection.');
    } finally {
      this.isLoading.set(false);
    }
  }
}

