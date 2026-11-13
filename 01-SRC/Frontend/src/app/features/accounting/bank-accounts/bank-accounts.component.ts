import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { BankCashAccountDto, CreateBankCashAccountRequest, UpdateBankCashAccountRequest } from '../../../core/contracts/finance.models';
import { FinanceApiService } from '../../../core/services/finance-api.service';
import { AccountingApiService } from '../../../core/services/accounting-api.service';
import { AccountDto } from '../../../core/contracts/accounting.models';

@Component({
  selector: 'magic-bank-accounts',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './bank-accounts.component.html',
  styleUrl: './bank-accounts.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BankAccountsComponent implements OnInit {
  private readonly financeApi = inject(FinanceApiService);
  private readonly accountingApi = inject(AccountingApiService);
  private readonly fb = inject(FormBuilder);

  protected readonly accounts = signal<readonly BankCashAccountDto[]>([]);
  protected readonly glAccounts = signal<readonly AccountDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  protected readonly isModalOpen = signal(false);
  protected readonly isRevealModalOpen = signal(false);
  protected readonly revealedAccount = signal<{ id: string; name: string; fullNumber: string } | null>(null);
  protected readonly editingAccount = signal<BankCashAccountDto | null>(null);
  protected readonly selectedFilter = signal<'ALL' | 'Bank' | 'Cash'>('ALL');
  protected readonly actionMessage = signal<{ type: 'ok' | 'error'; text: string } | null>(null);

  protected accountForm!: FormGroup;

  ngOnInit(): void {
    this.initForm();
    this.loadAccounts();
    this.loadGlAccounts();
  }

  private initForm(): void {
    this.accountForm = this.fb.group({
      accountName: ['', [Validators.required, Validators.maxLength(100)]],
      accountType: ['Bank', [Validators.required]],
      bankName: ['', [Validators.maxLength(100)]],
      branchName: ['', [Validators.maxLength(100)]],
      accountNumber: ['', [Validators.required, Validators.maxLength(50)]],
      ifscCode: ['', [Validators.maxLength(20)]],
      glAccountId: ['', [Validators.required]],
      isActive: [true],
    });
  }

  protected loadAccounts(): void {
    this.loading.set(true);
    const filter = this.selectedFilter();
    const typeParam = filter === 'ALL' ? undefined : filter;

    this.financeApi.getBankAccounts(typeParam).subscribe({
      next: (data) => {
        this.accounts.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.actionMessage.set({
          type: 'error',
          text: err?.error?.error?.detail || 'Failed to load bank and cash accounts.',
        });
        this.loading.set(false);
      },
    });
  }

  private loadGlAccounts(): void {
    this.accountingApi.getAccounts().subscribe({
      next: (accounts) => {
        // Filter asset accounts (cash and bank: 1111, 1112, etc.)
        this.glAccounts.set(accounts.filter((a) => a.rootClass === 'Asset'));
      },
      error: () => {},
    });
  }

  protected setFilter(type: 'ALL' | 'Bank' | 'Cash'): void {
    this.selectedFilter.set(type);
    this.loadAccounts();
  }

  protected openCreateModal(): void {
    this.editingAccount.set(null);
    this.accountForm.reset({
      accountName: '',
      accountType: 'Bank',
      bankName: '',
      branchName: '',
      accountNumber: '',
      ifscCode: '',
      glAccountId: '',
      isActive: true,
    });
    this.isModalOpen.set(true);
  }

  protected openEditModal(account: BankCashAccountDto): void {
    this.editingAccount.set(account);
    this.accountForm.patchValue({
      accountName: account.accountName,
      accountType: account.accountType,
      bankName: account.bankName,
      branchName: account.branchName,
      accountNumber: account.maskedAccountNumber,
      ifscCode: account.ifscCode,
      glAccountId: account.glAccountId,
      isActive: account.isActive,
    });
    this.isModalOpen.set(true);
  }

  protected closeModal(): void {
    this.isModalOpen.set(false);
    this.editingAccount.set(null);
  }

  protected saveAccount(): void {
    if (this.accountForm.invalid) {
      this.accountForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    this.actionMessage.set(null);
    const formVal = this.accountForm.value;

    const editing = this.editingAccount();
    if (editing) {
      const updateReq: UpdateBankCashAccountRequest = {
        accountName: formVal.accountName,
        bankName: formVal.bankName || undefined,
        branchName: formVal.branchName || undefined,
        ifscCode: formVal.ifscCode || undefined,
        isActive: formVal.isActive,
      };

      this.financeApi.updateBankAccount(editing.id, updateReq).subscribe({
        next: () => {
          this.saving.set(false);
          this.isModalOpen.set(false);
          this.actionMessage.set({ type: 'ok', text: `Account "${formVal.accountName}" updated successfully.` });
          this.loadAccounts();
        },
        error: (err) => {
          this.saving.set(false);
          this.actionMessage.set({
            type: 'error',
            text: err?.error?.error?.detail || 'Failed to update account.',
          });
        },
      });
    } else {
      const createReq: CreateBankCashAccountRequest = {
        accountName: formVal.accountName,
        accountType: formVal.accountType,
        bankName: formVal.bankName || undefined,
        branchName: formVal.branchName || undefined,
        accountNumber: formVal.accountNumber,
        ifscCode: formVal.ifscCode || undefined,
        glAccountId: formVal.glAccountId,
        openingBalancePaise: 0,
      };

      this.financeApi.createBankAccount(createReq).subscribe({
        next: () => {
          this.saving.set(false);
          this.isModalOpen.set(false);
          this.actionMessage.set({ type: 'ok', text: `Account "${formVal.accountName}" created successfully.` });
          this.loadAccounts();
        },
        error: (err) => {
          this.saving.set(false);
          this.actionMessage.set({
            type: 'error',
            text: err?.error?.error?.detail || 'Failed to create account.',
          });
        },
      });
    }
  }

  protected revealAccount(account: BankCashAccountDto): void {
    this.loading.set(true);
    this.financeApi.revealAccountNumber(account.id).subscribe({
      next: (res) => {
        this.loading.set(false);
        this.revealedAccount.set({
          id: account.id,
          name: account.accountName,
          fullNumber: res.accountNumber,
        });
        this.isRevealModalOpen.set(true);
      },
      error: (err) => {
        this.loading.set(false);
        this.actionMessage.set({
          type: 'error',
          text: err?.error?.error?.detail || 'Unauthorized: Bank account reveal requires elevated permissions (AUDIT logged).',
        });
      },
    });
  }

  protected closeRevealModal(): void {
    this.isRevealModalOpen.set(false);
    this.revealedAccount.set(null);
  }

  protected formatPaise(paise: number): string {
    const rupees = (paise / 100).toLocaleString('en-IN', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
    return `₹${rupees}`;
  }
}
