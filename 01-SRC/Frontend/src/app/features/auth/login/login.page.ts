import { CommonModule } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthBoundaryService } from '../../../core/services/auth-boundary.service';
import { IntroStateService } from '../../../core/services/intro-state.service';
import { BrandIntroComponent } from '../brand-intro/brand-intro.component';

/**
 * Enterprise Authentication Page for MAGIC ERP.
 * Plays the 5-second brand introduction on startup, then transitions to the
 * secure authentication form. Bypasses intro if already completed in current session.
 */
@Component({
  selector: 'magic-login',
  standalone: true,
  imports: [CommonModule, FormsModule, BrandIntroComponent],
  templateUrl: './login.page.html',
  styleUrl: './login.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage implements OnInit {
  private readonly auth = inject(AuthBoundaryService);
  private readonly introState = inject(IntroStateService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  protected readonly showIntro = signal<boolean>(!this.introState.hasCompletedIntro());
  protected username: string = '';
  protected password: string = '';
  protected readonly showPassword = signal<boolean>(false);
  protected readonly isLoading = signal<boolean>(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly isLockout = signal<boolean>(false);
  protected readonly sessionExpiredNotice = signal<boolean>(false);
  protected readonly accessDeniedNotice = signal<boolean>(false);

  private returnUrl: string = '/accounting/dashboard';

  ngOnInit(): void {
    // If already authenticated, redirect straight to dashboard
    if (this.auth.isAuthenticated()) {
      this.router.navigate(['/accounting/dashboard']);
      return;
    }

    this.route.queryParams.subscribe((params) => {
      if (params['returnUrl']) {
        this.returnUrl = params['returnUrl'];
      }
      if (params['sessionExpired'] === 'true') {
        this.sessionExpiredNotice.set(true);
        this.showIntro.set(false);
      }
      if (params['unauthorized'] === 'true') {
        this.accessDeniedNotice.set(true);
        this.showIntro.set(false);
      }
    });
  }

  protected onIntroFinished(): void {
    this.introState.markCompleted();
    this.showIntro.set(false);
  }

  protected togglePasswordVisibility(): void {
    this.showPassword.update((v) => !v);
  }

  protected fillCredentials(user: string, pass: string): void {
    this.username = user;
    this.password = pass;
    this.errorMessage.set(null);
  }

  protected async onSubmit(): Promise<void> {
    const userVal = this.username.trim();
    const passVal = this.password;

    if (!userVal || !passVal) {
      this.errorMessage.set('Please enter both username/email and password.');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set(null);
    this.isLockout.set(false);

    try {
      const result = await this.auth.login(userVal, passVal);
      if (result.success) {
        this.router.navigateByUrl(this.returnUrl);
      } else {
        this.errorMessage.set(result.error || 'Authentication failed.');
        if (result.lockout) {
          this.isLockout.set(true);
        }
      }
    } catch (err: any) {
      this.errorMessage.set(err.message || 'An unexpected error occurred. Please try again.');
    } finally {
      this.isLoading.set(false);
    }
  }
}
