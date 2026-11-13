import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  CreateSupplierRequest,
  GstRegistrationType,
  SupplierDetailDto,
  SupplierSummaryDto,
  UpdateSupplierRequest,
} from '../../../core/contracts/commercial.models';
import { CommercialApiService } from '../../../core/services/commercial-api.service';

@Component({
  selector: 'magic-suppliers-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './suppliers-list.component.html',
  styleUrl: './suppliers-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SuppliersListComponent implements OnInit {
  private readonly api = inject(CommercialApiService);

  protected readonly suppliers = signal<readonly SupplierSummaryDto[]>([]);
  protected readonly selectedSupplier = signal<SupplierDetailDto | null>(null);

  protected readonly loading = signal<boolean>(true);
  protected readonly saving = signal<boolean>(false);
  protected readonly isCreateOpen = signal<boolean>(false);
  protected readonly isEditOpen = signal<boolean>(false);
  protected readonly actionMessage = signal<{ type: 'ok' | 'error'; text: string } | null>(null);

  // Unmasked bank account storage (transient in-memory only, keyed by bankAccountId)
  protected readonly unmaskedAccounts = signal<Record<string, string>>({});
  protected readonly isUnmasking = signal<boolean>(false);

  // Pagination & Filtering
  protected filterSearch = '';
  protected filterCategory = '';
  protected filterMsme: boolean | undefined = undefined;
  protected filterActive: boolean | undefined = undefined;
  protected currentPage = 1;
  protected pageSize = 20;
  protected totalSuppliers = 0;

  // Supplier Form Model
  protected formModel: {
    id?: string;
    supplierCode: string;
    name: string;
    category: string;
    gstType: GstRegistrationType;
    gstin: string;
    pan: string;
    contactPerson: string;
    phone: string;
    email: string;
    paymentTerms: string;
    creditPeriodDays: number;
    isMsmeRegistered: boolean;
    msmeType: string;
    udyamRegistrationNumber: string;
    notes: string;
    // Primary Address
    addressLine1: string;
    city: string;
    state: string;
    pinCode: string;
    // Bank Account
    bankName: string;
    branchName: string;
    accountNumber: string;
    ifscCode: string;
    accountType: string;
  } = this.getEmptyForm();

  ngOnInit(): void {
    this.loadSuppliers();
  }

  protected loadSuppliers(): void {
    this.loading.set(true);
    this.api
      .getSuppliers({
        search: this.filterSearch.trim() || undefined,
        category: this.filterCategory || undefined,
        isMsme: this.filterMsme,
        isActive: this.filterActive,
        page: this.currentPage,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (res) => {
          this.suppliers.set(res.items);
          this.totalSuppliers = res.totalCount;
          this.loading.set(false);
        },
        error: (err) => {
          this.loading.set(false);
          this.actionMessage.set({
            type: 'error',
            text: err.error?.detail || 'Failed to load suppliers from PostgreSQL.',
          });
        },
      });
  }

  protected onSearchChange(): void {
    this.currentPage = 1;
    this.loadSuppliers();
  }

  protected viewSupplier(s: SupplierSummaryDto): void {
    this.unmaskedAccounts.set({});
    this.api.getSupplierById(s.id).subscribe({
      next: (detail) => this.selectedSupplier.set(detail),
      error: (err) => {
        this.actionMessage.set({
          type: 'error',
          text: err.error?.detail || 'Failed to load supplier details.',
        });
      },
    });
  }

  protected openCreate(): void {
    this.formModel = this.getEmptyForm();
    this.isCreateOpen.set(true);
    this.isEditOpen.set(false);
  }

  protected openEdit(detail: SupplierDetailDto): void {
    const primaryAddr = detail.addresses.find((a) => a.isPrimary) || detail.addresses[0];
    const primaryBank = detail.bankAccounts.find((b) => b.isPrimary) || detail.bankAccounts[0];

    this.formModel = {
      id: detail.id,
      supplierCode: detail.supplierCode,
      name: detail.name,
      category: detail.category,
      gstType: detail.gstType,
      gstin: detail.gstin || '',
      pan: detail.pan || '',
      contactPerson: detail.contactPerson || '',
      phone: detail.phone,
      email: detail.email || '',
      paymentTerms: detail.paymentTerms,
      creditPeriodDays: detail.creditPeriodDays,
      isMsmeRegistered: detail.isMsmeRegistered,
      msmeType: detail.msmeType || '',
      udyamRegistrationNumber: detail.udyamRegistrationNumber || '',
      notes: detail.notes || '',
      addressLine1: primaryAddr?.addressLine1 || '',
      city: primaryAddr?.city || '',
      state: primaryAddr?.state || 'Tamil Nadu',
      pinCode: primaryAddr?.pinCode || '',
      bankName: primaryBank?.bankName || '',
      branchName: primaryBank?.branchName || '',
      accountNumber: '',
      ifscCode: primaryBank?.ifscCode || '',
      accountType: primaryBank?.accountType || 'Current',
    };
    this.isEditOpen.set(true);
    this.isCreateOpen.set(false);
  }

  protected closeDrawers(): void {
    this.selectedSupplier.set(null);
    this.isCreateOpen.set(false);
    this.isEditOpen.set(false);
    this.unmaskedAccounts.set({});
  }

  protected saveSupplier(): void {
    if (!this.formModel.name.trim() || !this.formModel.phone.trim()) {
      this.actionMessage.set({ type: 'error', text: 'Supplier name and phone number are required.' });
      return;
    }

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

    if (this.isEditOpen() && this.formModel.id) {
      const updateReq: UpdateSupplierRequest = {
        name: this.formModel.name.trim(),
        category: this.formModel.category.trim() || 'General Vendor',
        gstType: this.formModel.gstType,
        gstin: gstin || null,
        pan: this.formModel.pan.trim().toUpperCase() || null,
        contactPerson: this.formModel.contactPerson.trim() || null,
        phone: this.formModel.phone.trim(),
        email: this.formModel.email.trim() || null,
        paymentTerms: this.formModel.paymentTerms.trim() || 'Net 30',
        creditPeriodDays: this.formModel.creditPeriodDays,
        msmeType: this.formModel.isMsmeRegistered ? this.formModel.msmeType.trim() || 'Micro' : null,
        udyamRegistrationNumber: this.formModel.isMsmeRegistered
          ? this.formModel.udyamRegistrationNumber.trim() || null
          : null,
        isMsmeRegistered: this.formModel.isMsmeRegistered,
        notes: this.formModel.notes.trim() || null,
        isActive: true,
        addresses: this.formModel.addressLine1
          ? [
              {
                addressType: 1,
                addressLine1: this.formModel.addressLine1.trim(),
                addressLine2: null,
                city: this.formModel.city.trim() || 'Coimbatore',
                state: this.formModel.state.trim() || 'Tamil Nadu',
                stateCode: '33',
                pinCode: this.formModel.pinCode.trim() || '641001',
                country: 'India',
                isPrimary: true,
              },
            ]
          : undefined,
        bankAccounts: this.formModel.accountNumber.trim()
          ? [
              {
                bankName: this.formModel.bankName.trim() || 'HDFC Bank',
                branchName: this.formModel.branchName.trim() || 'Main Branch',
                accountNumber: this.formModel.accountNumber.trim(),
                ifscCode: this.formModel.ifscCode.trim().toUpperCase() || 'HDFC0001234',
                accountType: this.formModel.accountType || 'Current',
                isPrimary: true,
              },
            ]
          : undefined,
      };

      this.api.updateSupplier(this.formModel.id, updateReq).subscribe({
        next: (saved) => {
          this.saving.set(false);
          this.closeDrawers();
          this.actionMessage.set({
            type: 'ok',
            text: `Supplier "${saved.name}" (${saved.supplierCode}) updated successfully.`,
          });
          this.loadSuppliers();
        },
        error: (err) => {
          this.saving.set(false);
          this.actionMessage.set({
            type: 'error',
            text: err.error?.detail || 'Failed to update supplier.',
          });
        },
      });
    } else {
      const createReq: CreateSupplierRequest = {
        supplierCode: this.formModel.supplierCode.trim() || `SUPP-${Date.now().toString().slice(-4)}`,
        name: this.formModel.name.trim(),
        category: this.formModel.category.trim() || 'General Vendor',
        gstType: this.formModel.gstType,
        gstin: gstin || null,
        pan: this.formModel.pan.trim().toUpperCase() || null,
        contactPerson: this.formModel.contactPerson.trim() || null,
        phone: this.formModel.phone.trim(),
        email: this.formModel.email.trim() || null,
        paymentTerms: this.formModel.paymentTerms.trim() || 'Net 30',
        creditPeriodDays: this.formModel.creditPeriodDays,
        msmeType: this.formModel.isMsmeRegistered ? this.formModel.msmeType.trim() || 'Micro' : null,
        udyamRegistrationNumber: this.formModel.isMsmeRegistered
          ? this.formModel.udyamRegistrationNumber.trim() || null
          : null,
        isMsmeRegistered: this.formModel.isMsmeRegistered,
        notes: this.formModel.notes.trim() || null,
        addresses: this.formModel.addressLine1
          ? [
              {
                addressType: 1,
                addressLine1: this.formModel.addressLine1.trim(),
                addressLine2: null,
                city: this.formModel.city.trim() || 'Coimbatore',
                state: this.formModel.state.trim() || 'Tamil Nadu',
                stateCode: '33',
                pinCode: this.formModel.pinCode.trim() || '641001',
                country: 'India',
                isPrimary: true,
              },
            ]
          : undefined,
        bankAccounts: this.formModel.accountNumber.trim()
          ? [
              {
                bankName: this.formModel.bankName.trim() || 'HDFC Bank',
                branchName: this.formModel.branchName.trim() || 'Main Branch',
                accountNumber: this.formModel.accountNumber.trim(),
                ifscCode: this.formModel.ifscCode.trim().toUpperCase() || 'HDFC0001234',
                accountType: this.formModel.accountType || 'Current',
                isPrimary: true,
              },
            ]
          : undefined,
      };

      this.api.createSupplier(createReq).subscribe({
        next: (created) => {
          this.saving.set(false);
          this.closeDrawers();
          this.actionMessage.set({
            type: 'ok',
            text: `Supplier "${created.name}" (${created.supplierCode}) registered with encrypted banking details.`,
          });
          this.loadSuppliers();
        },
        error: (err) => {
          this.saving.set(false);
          this.actionMessage.set({
            type: 'error',
            text: err.error?.detail || 'Failed to create supplier.',
          });
        },
      });
    }
  }

  // Authorized reveal of bank account number with server-side audit record
  protected revealBankAccount(bankAccountId: string): void {
    const supp = this.selectedSupplier();
    if (!supp) return;

    if (!confirm('Authorized Action: Reveal unmasked bank account number for vendor remittance? This access is recorded in tamper-evident security audit logs.')) {
      return;
    }

    this.isUnmasking.set(true);
    this.api.getUnmaskedBankAccount(supp.id, bankAccountId).subscribe({
      next: (unmasked) => {
        this.isUnmasking.set(false);
        this.unmaskedAccounts.update((prev) => ({
          ...prev,
          [bankAccountId]: unmasked.plaintextAccountNumber,
        }));
        this.actionMessage.set({
          type: 'ok',
          text: `Bank details unmasked. Security audit record created for user session.`,
        });

        // Auto-remask after 30 seconds
        setTimeout(() => {
          this.unmaskedAccounts.update((prev) => {
            const next = { ...prev };
            delete next[bankAccountId];
            return next;
          });
        }, 30_000);
      },
      error: (err) => {
        this.isUnmasking.set(false);
        this.actionMessage.set({
          type: 'error',
          text: err.error?.detail || 'Access denied: You lack permission commercial:suppliers:bank-details.',
        });
      },
    });
  }

  private getEmptyForm() {
    return {
      supplierCode: '',
      name: '',
      category: 'Raw Materials',
      gstType: GstRegistrationType.Registered,
      gstin: '',
      pan: '',
      contactPerson: '',
      phone: '',
      email: '',
      paymentTerms: 'Net 30',
      creditPeriodDays: 30,
      isMsmeRegistered: false,
      msmeType: 'Micro',
      udyamRegistrationNumber: '',
      notes: '',
      addressLine1: '',
      city: '',
      state: 'Tamil Nadu',
      pinCode: '',
      bankName: '',
      branchName: '',
      accountNumber: '',
      ifscCode: '',
      accountType: 'Current',
    };
  }
}
