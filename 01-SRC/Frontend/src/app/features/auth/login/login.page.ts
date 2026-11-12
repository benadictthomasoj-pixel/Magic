import { ChangeDetectionStrategy, Component } from '@angular/core';
import { BrandIntroComponent } from '../brand-intro/brand-intro.component';

/**
 * Brand Showcase Page for MAGIC ERP.
 * Hosts the cinematic animated logo experience.
 * The unnecessary login card has been eliminated.
 */
@Component({
  selector: 'magic-login',
  standalone: true,
  imports: [BrandIntroComponent],
  templateUrl: './login.page.html',
  styleUrl: './login.page.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginPage {}
