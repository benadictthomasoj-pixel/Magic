import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

@Component({
  selector: 'magic-billing-feature-placeholder',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './billing-feature-placeholder.component.html',
  styleUrl: './billing-feature-placeholder.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BillingFeaturePlaceholderComponent {
  private readonly route = inject(ActivatedRoute);

  protected get featureTitle(): string {
    return this.route.snapshot.data['title'] || 'Billing Feature';
  }

  protected get featureCategory(): string {
    return this.route.snapshot.data['category'] || 'Commercial Operations';
  }

  protected get featureIcon(): string {
    return this.route.snapshot.data['icon'] || '📦';
  }

  protected get featureDescription(): string {
    return (
      this.route.snapshot.data['description'] ||
      'This commercial operation capability is designed as part of Phase 3 Commercial Suite, integrating seamlessly with the Phase 2 Accounting engine.'
    );
  }

  protected get plannedScope(): readonly string[] {
    return (
      this.route.snapshot.data['scope'] || [
        'Direct integration with PostgreSQL General Ledger and Subledgers.',
        'Deterministic GST tax calculations and invoice numbering.',
        'Strict Online LAN with edge caching support.',
      ]
    );
  }
}
