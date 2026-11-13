import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  ServiceCatalogDto,
  ServiceRequestDto,
  ServiceSummaryDto,
} from '../../../core/contracts/operations.models';
import { OperationsApiService } from '../../../core/services/operations-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'magic-service-desk',
  standalone: true,
  imports: [CommonModule, FormsModule, StatusBadgeComponent],
  templateUrl: './service-desk.component.html',
  styleUrl: './service-desk.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ServiceDeskComponent implements OnInit {
  private readonly api = inject(OperationsApiService);

  protected readonly activeTab = signal<'requests' | 'catalog'>('requests');
  protected readonly summary = signal<ServiceSummaryDto | null>(null);
  protected readonly requests = signal<readonly ServiceRequestDto[]>([]);
  protected readonly catalog = signal<readonly ServiceCatalogDto[]>([]);
  protected readonly selectedRequest = signal<ServiceRequestDto | null>(null);
  protected readonly loading = signal(true);

  protected readonly showNewRequestModal = signal(false);
  protected readonly showQuoteModal = signal(false);
  protected readonly showPartModal = signal(false);

  protected filterStatus = '';

  protected newRequest = {
    customerId: '11111111-1111-1111-1111-111111111111',
    itemDescription: 'Thermal POS Printer Model TP-80',
    serialNumber: 'SN-TP80-9941',
    reportedIssue: 'Paper cutter jam and feed gear error',
    priority: 'High',
  };

  protected newQuote = {
    laborDescription: 'Disassembly and Cutter Mechanism Service',
    laborRateRupees: 1500,
    partDescription: 'Replacement Cutter Blade Assembly',
    partRateRupees: 2500,
  };

  protected partData = {
    productId: '11111111-1111-1111-1111-111111111111',
    warehouseId: '11111111-1111-1111-1111-111111111111',
    quantity: 1,
  };

  ngOnInit(): void {
    this.loadSummary();
    this.loadRequests();
    this.loadCatalog();
  }

  protected setTab(tab: 'requests' | 'catalog'): void {
    this.activeTab.set(tab);
  }

  protected loadSummary(): void {
    this.api.getServiceSummary().subscribe({
      next: (res) => this.summary.set(res),
      error: () => {},
    });
  }

  protected loadRequests(): void {
    this.loading.set(true);
    this.api.getServiceRequests({ status: this.filterStatus || undefined }).subscribe({
      next: (res) => {
        this.requests.set(res.items);
        if (res.items.length > 0 && !this.selectedRequest()) {
          this.selectRequest(res.items[0]);
        }
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadCatalog(): void {
    this.api.getServiceCatalog().subscribe({
      next: (res) => this.catalog.set(res),
      error: () => {},
    });
  }

  protected selectRequest(req: ServiceRequestDto): void {
    this.api.getServiceRequestById(req.id).subscribe({
      next: (res) => this.selectedRequest.set(res),
      error: () => this.selectedRequest.set(req),
    });
  }

  protected submitRequest(): void {
    this.api
      .createServiceRequest({
        customerId: this.newRequest.customerId,
        itemDescription: this.newRequest.itemDescription,
        serialNumber: this.newRequest.serialNumber,
        reportedIssue: this.newRequest.reportedIssue,
        priority: this.newRequest.priority,
      })
      .subscribe({
        next: (res) => {
          this.showNewRequestModal.set(false);
          this.loadRequests();
          this.loadSummary();
          this.selectRequest(res);
        },
      });
  }

  protected updateStatus(reqId: string, status: string): void {
    this.api
      .updateServiceRequestStatus(reqId, {
        status,
        technicianNotes: `Status updated to ${status}`,
      })
      .subscribe({
        next: (res) => {
          this.loadRequests();
          this.loadSummary();
          this.selectedRequest.set(res);
        },
      });
  }

  protected submitQuotation(): void {
    const req = this.selectedRequest();
    if (!req) return;

    this.api
      .createQuotation(req.id, {
        lines: [
          {
            lineType: 'Labor',
            description: this.newQuote.laborDescription,
            quantity: 1,
            unitRatePaise: Math.round(this.newQuote.laborRateRupees * 100),
          },
          {
            lineType: 'Part',
            description: this.newQuote.partDescription,
            quantity: 1,
            unitRatePaise: Math.round(this.newQuote.partRateRupees * 100),
          },
        ],
      })
      .subscribe({
        next: () => {
          this.showQuoteModal.set(false);
          this.selectRequest(req);
        },
      });
  }

  protected approveQuotation(quoteId: string): void {
    this.api.approveQuotation(quoteId).subscribe({
      next: () => {
        const req = this.selectedRequest();
        if (req) this.selectRequest(req);
      },
    });
  }

  protected submitConsumePart(): void {
    const req = this.selectedRequest();
    if (!req) return;

    this.api
      .consumePart(req.id, {
        productId: this.partData.productId,
        warehouseId: this.partData.warehouseId,
        quantity: this.partData.quantity,
      })
      .subscribe({
        next: () => {
          this.showPartModal.set(false);
          this.selectRequest(req);
        },
      });
  }

  protected invoiceRequest(req: ServiceRequestDto): void {
    this.api
      .invoiceServiceRequest(req.id, {
        paymentMethod: 'Cash',
        notes: `Settled at desk for ticket ${req.requestNumber}`,
      })
      .subscribe({
        next: (invId) => {
          alert(`Service Invoice created successfully: ${invId}`);
          this.loadRequests();
          this.loadSummary();
          this.selectRequest(req);
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

