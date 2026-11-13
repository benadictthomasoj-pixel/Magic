import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  CreateCustomerRequest,
  CustomerDetailDto,
  CustomerGroupDto,
  CustomerSummaryDto,
  CustomerType,
  GstRegistrationType,
  UpdateCreditLimitRequest,
  UpdateCustomerRequest,
} from '../../../core/contracts/commercial.models';
import { CommercialApiService } from '../../../core/services/commercial-api.service';

@Component({
  selector: 'magic-customers-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './customers-list.component.html',
  styleUrl: './customers-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CustomersListComponent implements OnInit {
  private readonly api = inject(CommercialApiService);

  protected readonly customers = signal<readonly CustomerSummaryDto[]>([]);
  protected readonly customerGroups = signal<readonly CustomerGroupDto[]>([]);
  protected readonly selectedCustomer = signal<CustomerDetailDto | null>(null);

  protected readonly loading = signal<boolean>(true);
  protected readonly saving = signal<boolean>(false);
  protected readonly isCreateOpen = signal<boolean>(false);
  protected readonly isEditOpen = signal<boolean>(false);
  protected readonly isCreditLimitOpen = signal<boolean>(false);
  protected readonly actionMessage = signal<{ type: 'ok' | 'error'; text: string } | null>(null);

  // Pagination & Filtering
  protected filterSearch = '';
  protected filterCustomerType: CustomerType | undefined = undefined;
  protected filterGstType: GstRegistrationType | undefined = undefined;
  protected filterActive: boolean | undefined = undefined;
  protected currentPage = 1;
  protected pageSize = 20;
  protected totalCustomers = 0;

  // Customer Form Model
  protected formModel: {
    id?: string;
    customerCode: string;
    name: string;
    customerType: CustomerType;
    gstType: GstRegistrationType;
    gstin: string;
    pan: string;
    contactPerson: string;
    phone: string;
    email: string;
    customerGroupId: string;
    creditLimitRupees: number;
    creditPeriodDays: number;
    notes: string;
    addressLine1: string;
    city: string;
    state: string;
    stateCode: string;
    pinCode: string;
  } = this.getEmptyForm();

  // Credit Limit Update Model
  protected creditLimitModel = {
    newCreditLimitRupees: 0,
    creditPeriodDays: 30,
    isCreditHold: false,
    reason: '',
  };

  ngOnInit(): void {
    this.loadGroups();
    this.loadCustomers();
  }

  protected loadGroups(): void {
    this.api.getCustomerGroups().subscribe({
      next: (groups) => this.customerGroups.set(groups),
      error: () => {},
    });
  }

  protected loadCustomers(): void {
    this.loading.set(true);
    this.api
      .getCustomers({
        search: this.filterSearch.trim() || undefined,
        customerType: this.filterCustomerType,
        gstType: this.filterGstType,
        isActive: this.filterActive,
        page: this.currentPage,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (res) => {
          this.customers.set(res.items);
          this.totalCustomers = res.totalCount;
          this.loading.set(false);
        },
        error: (err) => {
          this.loading.set(false);
          this.actionMessage.set({
            type: 'error',
            text: err.error?.detail || 'Failed to load customers from PostgreSQL.',
          });
        },
      });
  }

  protected onSearchChange(): void {
    this.currentPage = 1;
    this.loadCustomers();
  }

  protected viewCustomer(c: CustomerSummaryDto): void {
    this.api.getCustomerById(c.id).subscribe({
      next: (detail) => this.selectedCustomer.set(detail),
      error: (err) => {
        this.actionMessage.set({
          type: 'error',
          text: err.error?.detail || 'Failed to load customer profile.',
        });
      },
    });
  }

  protected openCreate(): void {
    this.formModel = this.getEmptyForm();
    if (this.customerGroups().length > 0) {
      this.formModel.customerGroupId = this.customerGroups()[0].id;
    }
    this.isCreateOpen.set(true);
    this.isEditOpen.set(false);
  }

  protected openEdit(detail: CustomerDetailDto): void {
    const primaryAddr = detail.addresses.find((a) => a.isDefaultBilling) || detail.addresses[0];
    this.formModel = {
      id: detail.id,
      customerCode: detail.customerCode,
      name: detail.name,
      customerType: detail.customerType,
      gstType: detail.gstType,
      gstin: detail.gstin || '',
      pan: detail.pan || '',
      contactPerson: detail.contactPerson || '',
      phone: detail.phone,
      email: detail.email || '',
      customerGroupId: detail.customerGroupId || '',
      creditLimitRupees: detail.creditLimitPaise / 100,
      creditPeriodDays: detail.creditPeriodDays,
      notes: detail.notes || '',
      addressLine1: primaryAddr?.addressLine1 || '',
      city: primaryAddr?.city || '',
      state: primaryAddr?.state || 'Tamil Nadu',
      stateCode: primaryAddr?.stateCode || '33',
      pinCode: primaryAddr?.pinCode || '',
    };
    this.isEditOpen.set(true);
    this.isCreateOpen.set(false);
  }

  protected openCreditLimitDrawer(detail: CustomerDetailDto): void {
    this.creditLimitModel = {
      newCreditLimitRupees: detail.creditLimitPaise / 100,
      creditPeriodDays: detail.creditPeriodDays,
      isCreditHold: detail.isCreditHold,
      reason: '',
    };
    this.isCreditLimitOpen.set(true);
  }

  protected closeDrawers(): void {
    this.selectedCustomer.set(null);
    this.isCreateOpen.set(false);
    this.isEditOpen.set(false);
    this.isCreditLimitOpen.set(false);
  }

  protected saveCustomer(): void {
    if (!this.formModel.name.trim() || !this.formModel.phone.trim()) {
      this.actionMessage.set({ type: 'error', text: 'Customer name and phone number are required.' });
      return;
    }

    // Client-side GSTIN format validation (if provided)
    const gstin = this.formModel.gstin.trim().toUpperCase();
    if (gstin && !/^[0-9]{2}[A-Z]{5}[0-9]{4}[A-Z]{1}[1-9A-Z]{1}Z[0-9A-Z]{1}$/.test(gstin)) {
      this.actionMessage.set({
        type: 'error',
        text: 'Invalid GSTIN format. Expected 15-character alphanumeric (e.g. 33AAAAA0000A1Z5).',
      });
      return;
    }

    this.saving.set(true);
    this.actionMessage.set(null);

    const creditLimitPaise = Math.round(this.formModel.creditLimitRupees * 100);

    if (this.isEditOpen() && this.formModel.id) {
      const updateReq: UpdateCustomerRequest = {
        name: this.formModel.name.trim(),
        customerType: this.formModel.customerType,
        gstType: this.formModel.gstType,
        gstin: gstin || null,
        pan: this.formModel.pan.trim().toUpperCase() || null,
        contactPerson: this.formModel.contactPerson.trim() || null,
        phone: this.formModel.phone.trim(),
        email: this.formModel.email.trim() || null,
        customerGroupId: this.formModel.customerGroupId || null,
        assignedBranchId: null,
        notes: this.formModel.notes.trim() || null,
        isActive: true,
        addresses: this.formModel.addressLine1
          ? [
              {
                addressType: 3,
                addressLine1: this.formModel.addressLine1.trim(),
                addressLine2: null,
                city: this.formModel.city.trim() || 'Coimbatore',
                district: null,
                state: this.formModel.state.trim() || 'Tamil Nadu',
                stateCode: this.formModel.stateCode.trim() || '33',
                pinCode: this.formModel.pinCode.trim() || '641001',
                country: 'India',
                isDefaultBilling: true,
                isDefaultShipping: true,
              },
            ]
          : undefined,
      };

      this.api.updateCustomer(this.formModel.id, updateReq).subscribe({
        next: (saved) => {
          this.saving.set(false);
          this.closeDrawers();
          this.actionMessage.set({
            type: 'ok',
            text: `Customer "${saved.name}" (${saved.customerCode}) updated successfully.`,
          });
          this.loadCustomers();
        },
        error: (err) => {
          this.saving.set(false);
          this.actionMessage.set({
            type: 'error',
            text: err.error?.detail || 'Failed to update customer.',
          });
        },
      });
    } else {
      const createReq: CreateCustomerRequest = {
        customerCode: this.formModel.customerCode.trim() || `CUST-${Date.now().toString().slice(-4)}`,
        name: this.formModel.name.trim(),
        customerType: this.formModel.customerType,
        gstType: this.formModel.gstType,
        gstin: gstin || null,
        pan: this.formModel.pan.trim().toUpperCase() || null,
        contactPerson: this.formModel.contactPerson.trim() || null,
        phone: this.formModel.phone.trim(),
        email: this.formModel.email.trim() || null,
        customerGroupId: this.formModel.customerGroupId || null,
        assignedBranchId: null,
        creditLimitPaise,
        creditPeriodDays: this.formModel.creditPeriodDays,
        notes: this.formModel.notes.trim() || null,
        addresses: this.formModel.addressLine1
          ? [
              {
                addressType: 3,
                addressLine1: this.formModel.addressLine1.trim(),
                addressLine2: null,
                city: this.formModel.city.trim() || 'Coimbatore',
                district: null,
                state: this.formModel.state.trim() || 'Tamil Nadu',
                stateCode: this.formModel.stateCode.trim() || '33',
                pinCode: this.formModel.pinCode.trim() || '641001',
                country: 'India',
                isDefaultBilling: true,
                isDefaultShipping: true,
              },
            ]
          : undefined,
      };

      this.api.createCustomer(createReq).subscribe({
        next: (created) => {
          this.saving.set(false);
          this.closeDrawers();
          this.actionMessage.set({
            type: 'ok',
            text: `Customer "${created.name}" (${created.customerCode}) created successfully.`,
          });
          this.loadCustomers();
        },
        error: (err) => {
          this.saving.set(false);
          this.actionMessage.set({
            type: 'error',
            text: err.error?.detail || 'Failed to create customer.',
          });
        },
      });
    }
  }

  protected submitCreditLimitUpdate(): void {
    const cust = this.selectedCustomer();
    if (!cust) return;

    if (!this.creditLimitModel.reason.trim()) {
      this.actionMessage.set({ type: 'error', text: 'Audit reason is required to modify credit limits.' });
      return;
    }

    this.saving.set(true);
    const req: UpdateCreditLimitRequest = {
      newCreditLimitPaise: Math.round(this.creditLimitModel.newCreditLimitRupees * 100),
      creditPeriodDays: this.creditLimitModel.creditPeriodDays,
      isCreditHold: this.creditLimitModel.isCreditHold,
      reason: this.creditLimitModel.reason.trim(),
    };

    this.api.updateCreditLimit(cust.id, req).subscribe({
      next: () => {
        this.saving.set(false);
        this.isCreditLimitOpen.set(false);
        this.actionMessage.set({
          type: 'ok',
          text: `Credit limit updated for customer "${cust.name}". Audit event recorded.`,
        });
        // Refresh details
        this.viewCustomer({ id: cust.id } as CustomerSummaryDto);
        this.loadCustomers();
      },
      error: (err) => {
        this.saving.set(false);
        this.actionMessage.set({
          type: 'error',
          text: err.error?.detail || 'Failed to update credit limit. Check permissions.',
        });
      },
    });
  }

  protected getCustomerTypeLabel(type: CustomerType): string {
    switch (type) {
      case CustomerType.Individual:
        return 'Individual';
      case CustomerType.Retail:
        return 'Retail';
      case CustomerType.Wholesale:
        return 'Wholesale';
      case CustomerType.Business:
        return 'B2B Corporate';
      default:
        return 'General';
    }
  }

  protected getGstTypeLabel(type: GstRegistrationType): string {
    switch (type) {
      case GstRegistrationType.Registered:
        return 'Registered Regular';
      case GstRegistrationType.Unregistered:
        return 'Unregistered / Consumer';
      case GstRegistrationType.Composition:
        return 'Composition Scheme';
      case GstRegistrationType.Overseas:
        return 'Overseas';
      case GstRegistrationType.SEZ:
        return 'Special Economic Zone (SEZ)';
      default:
        return 'Regular';
    }
  }

  private getEmptyForm() {
    return {
      customerCode: '',
      name: '',
      customerType: CustomerType.Retail,
      gstType: GstRegistrationType.Unregistered,
      gstin: '',
      pan: '',
      contactPerson: '',
      phone: '',
      email: '',
      customerGroupId: '',
      creditLimitRupees: 0,
      creditPeriodDays: 0,
      notes: '',
      addressLine1: '',
      city: '',
      state: 'Tamil Nadu',
      stateCode: '33',
      pinCode: '',
    };
  }
}

