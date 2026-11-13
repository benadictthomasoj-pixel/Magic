import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  CampaignDto,
  CustomerSegmentDto,
  MarketingSummaryDto,
  PromotionCouponDto,
} from '../../../core/contracts/operations.models';
import { OperationsApiService } from '../../../core/services/operations-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'magic-marketing-management',
  standalone: true,
  imports: [CommonModule, FormsModule, StatusBadgeComponent],
  templateUrl: './marketing-management.component.html',
  styleUrl: './marketing-management.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MarketingManagementComponent implements OnInit {
  private readonly api = inject(OperationsApiService);

  protected readonly activeTab = signal<'campaigns' | 'coupons' | 'segments'>('campaigns');
  protected readonly summary = signal<MarketingSummaryDto | null>(null);
  protected readonly campaigns = signal<readonly CampaignDto[]>([]);
  protected readonly coupons = signal<readonly PromotionCouponDto[]>([]);
  protected readonly segments = signal<readonly CustomerSegmentDto[]>([]);
  protected readonly loading = signal(true);

  protected readonly showNewCampaignModal = signal(false);
  protected readonly showNewCouponModal = signal(false);

  protected newCampaign = {
    name: '',
    channel: 'SMS',
    startDate: new Date().toISOString().split('T')[0],
    endDate: new Date(Date.now() + 30 * 86400000).toISOString().split('T')[0],
    budgetRupees: 10000,
  };

  protected newCoupon = {
    couponCode: '',
    discountType: 'Percentage',
    discountValue: 10,
    minOrderRupees: 500,
    maxDiscountRupees: 100,
    validFrom: new Date().toISOString().split('T')[0],
    validTo: new Date(Date.now() + 60 * 86400000).toISOString().split('T')[0],
    maxUsageCount: 500,
  };

  ngOnInit(): void {
    this.loadSummary();
    this.loadCampaigns();
    this.loadCoupons();
    this.loadSegments();
  }

  protected setTab(tab: 'campaigns' | 'coupons' | 'segments'): void {
    this.activeTab.set(tab);
  }

  protected loadSummary(): void {
    this.api.getMarketingSummary().subscribe({
      next: (res) => this.summary.set(res),
      error: () => {},
    });
  }

  protected loadCampaigns(): void {
    this.loading.set(true);
    this.api.getCampaigns().subscribe({
      next: (res) => {
        this.campaigns.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadCoupons(): void {
    this.api.getCoupons().subscribe({
      next: (res) => this.coupons.set(res),
      error: () => {},
    });
  }

  protected loadSegments(): void {
    this.api.getSegments().subscribe({
      next: (res) => this.segments.set(res),
      error: () => {},
    });
  }

  protected submitCampaign(): void {
    if (!this.newCampaign.name) return;

    this.api
      .createCampaign({
        name: this.newCampaign.name,
        channel: this.newCampaign.channel,
        startDate: this.newCampaign.startDate,
        endDate: this.newCampaign.endDate,
        budgetPaise: Math.round(this.newCampaign.budgetRupees * 100),
      })
      .subscribe({
        next: () => {
          this.showNewCampaignModal.set(false);
          this.loadCampaigns();
          this.loadSummary();
        },
      });
  }

  protected submitCoupon(): void {
    if (!this.newCoupon.couponCode) return;

    this.api
      .createCoupon({
        couponCode: this.newCoupon.couponCode.toUpperCase().trim(),
        discountType: this.newCoupon.discountType,
        discountValue: this.newCoupon.discountValue,
        minOrderAmountPaise: Math.round(this.newCoupon.minOrderRupees * 100),
        maxDiscountAmountPaise: Math.round(this.newCoupon.maxDiscountRupees * 100),
        validFromUtc: new Date(this.newCoupon.validFrom).toISOString(),
        validToUtc: new Date(this.newCoupon.validTo).toISOString(),
        maxUsageCount: this.newCoupon.maxUsageCount,
      })
      .subscribe({
        next: () => {
          this.showNewCouponModal.set(false);
          this.loadCoupons();
          this.loadSummary();
        },
      });
  }

  protected formatPaise(paise?: number): string {
    if (paise == null) return '0.00';
    return (paise / 100).toLocaleString('en-IN', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
  }
}

