import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import {
  BankCashAccountDto,
  CreateContraTransactionRequest,
  CreateTreasuryAdjustmentRequest,
  TreasuryTransactionDto,
} from '../../../core/contracts/finance.models';
import { FinanceApiService } from '../../../core/services/finance-api.service';
import { AccountingApiService } from '../../../core/services/accounting-api.service';
import { AccountDto } from '../../../core/contracts/accounting.models';

@Component({
  selector: 'magic-treasury-management',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './treasury-management.component.html',
  styleUrl: './treasury-management.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TreasuryManagementComponent implements OnInit {
  private readonly financeApi = inject(FinanceApiService);
  private readonly accountingApi = inject(AccountingApiService);
  private readonly fb = inject(FormBuilder);

  protected readonly transactions = signal<readonly TreasuryTransactionDto[]>([]);
  protected readonly accounts = signal<readonly BankCashAccountDto[]>([]);
  protected readonly expenseGlAccounts = signal<readonly AccountDto[]>([]);
  protected readonly incomeGlAccounts = signal<readonly AccountDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly submitting = signal(false);
  protected readonly isModalOpen = signal(false);
  protected readonly modalMode = signal<'contra' | 'adjustment'>('contra');
  protected readonly actionMessage = signal<{ type: 'ok' | 'error'; text: string } | null>(null);

  protected filterType = '';
  protected filterFromDate = '';
  protected filterToDate = '';

  protected contraForm!: FormGroup;
  protected adjustmentForm!: FormGroup;

  ngOnInit(): void {
    this.initForms();
    this.loadAccounts();
    this.loadGlAccounts();
    this.loadTransactions();
  }

  private initForms(): void {
    const today = new Date().toISOString().slice(0, 10);

    this.contraForm = this.fb.group({
      transactionType: ['CashDeposit', [Validators.required]],
      transactionDate: [today, [Validators.required]],
      fromAccountId: ['', [Validators.required]],
      toAccountId: ['', [Validators.required]],
      amountRupees: [null, [Validators.required, Validators.min(0.01)]],
      referenceNumber: ['', [Validators.maxLength(50)]],
      description: ['', [Validators.required, Validators.maxLength(500)]],
    });

    this.adjustmentForm = this.fb.group({
      adjustmentType: ['BankCharges', [Validators.required]],
      transactionDate: [today, [Validators.required]],
      bankAccountId: ['', [Validators.required]],
      contraLedgerAccountId: ['', [Validators.required]],
      amountRupees: [null, [Validators.required, Validators.min(0.01)]],
      referenceNumber: ['', [Validators.maxLength(50)]],
      description: ['', [Validators.required, Validators.maxLength(500)]],
    });
  }

  protected loadTransactions(): void {
    this.loading.set(true);
    this.financeApi
      .getTreasuryTransactions({
        transactionType: this.filterType || undefined,
        fromDate: this.filterFromDate || undefined,
        toDate: this.filterToDate || undefined,
      })
      .subscribe({
        next: (res) => {
          this.transactions.set(res.items);
          this.loading.set(false);
        },
        error: (err) => {
          this.actionMessage.set({
            type: 'error',
            text: err?.error?.error?.detail || 'Failed to load treasury transactions.',
          });
          this.loading.set(false);
        },
      });
  }

  private loadAccounts(): void {
    this.financeApi.getBankAccounts(undefined, true).subscribe({
      next: (data) => this.accounts.set(data),
      error: () => {},
    });
  }

  private loadGlAccounts(): void {
    this.accountingApi.getAccounts().subscribe({
      next: (accounts) => {
        this.expenseGlAccounts.set(accounts.filter((a) => a.rootClass === 'Expense'));
        this.incomeGlAccounts.set(accounts.filter((a) => a.rootClass === 'Revenue'));
      },
      error: () => {},
    });
  }

  protected openContraModal(): void {
    this.modalMode.set('contra');
    this.contraForm.reset({
      transactionType: 'CashDeposit',
      transactionDate: new Date().toISOString().slice(0, 10),
      fromAccountId: '',
      toAccountId: '',
      amountRupees: null,
      referenceNumber: '',
      description: 'Cash deposited into operating bank account',
    });
    this.isModalOpen.set(true);
  }

  protected openAdjustmentModal(): void {
    this.modalMode.set('adjustment');
    this.adjustmentForm.reset({
      adjustmentType: 'BankCharges',
      transactionDate: new Date().toISOString().slice(0, 10),
      bankAccountId: '',
      contraLedgerAccountId: '',
      amountRupees: null,
      referenceNumber: '',
      description: 'Bank monthly maintenance charges',
    });
    this.isModalOpen.set(true);
  }

  protected closeModal(): void {
    this.isModalOpen.set(false);
  }

  protected onContraTypeChange(): void {
    const type = this.contraForm.get('transactionType')?.value;
    if (type === 'CashDeposit') {
      const cashAcc = this.accounts().find((a) => a.accountType === 'Cash');
      const bankAcc = this.accounts().find((a) => a.accountType === 'Bank');
      this.contraForm.patchValue({
        fromAccountId: cashAcc?.id || '',
        toAccountId: bankAcc?.id || '',
        description: 'Cash deposited into bank account',
      });
    } else if (type === 'CashWithdrawal') {
      const cashAcc = this.accounts().find((a) => a.accountType === 'Cash');
      const bankAcc = this.accounts().find((a) => a.accountType === 'Bank');
      this.contraForm.patchValue({
        fromAccountId: bankAcc?.id || '',
        toAccountId: cashAcc?.id || '',
        description: 'Cash withdrawn for office petty cash vault',
      });
    } else {
      this.contraForm.patchValue({
        fromAccountId: '',
        toAccountId: '',
        description: 'Inter-bank fund transfer',
      });
    }
  }

  protected submitContra(): void {
    if (this.contraForm.invalid) {
      this.contraForm.markAllAsTouched();
      return;
    }

    const val = this.contraForm.value;
    if (val.fromAccountId === val.toAccountId) {
      alert('Source and destination accounts must be different for a Contra transaction.');
      return;
    }

    this.submitting.set(true);
    this.actionMessage.set(null);

    const amountPaise = Math.round(Number(val.amountRupees) * 100);
    const req: CreateContraTransactionRequest = {
      transactionType: val.transactionType,
      transactionDate: val.transactionDate,
      sourceAccountId: val.fromAccountId,
      destinationAccountId: val.toAccountId,
      amountPaise: amountPaise,
      referenceNumber: val.referenceNumber || undefined,
      narration: val.description,
    };

    this.financeApi.createContra(req).subscribe({
      next: (created) => {
        this.submitting.set(false);
        this.isModalOpen.set(false);
        this.actionMessage.set({
          type: 'ok',
          text: `Contra transaction "${created.transactionNumber}" executed. Double-entry voucher posted to GL.`,
        });
        this.loadTransactions();
        this.loadAccounts();
      },
      error: (err) => {
        this.submitting.set(false);
        this.actionMessage.set({
          type: 'error',
          text: err?.error?.error?.detail || 'Failed to execute Contra transaction.',
        });
      },
    });
  }

  protected submitAdjustment(): void {
    if (this.adjustmentForm.invalid) {
      this.adjustmentForm.markAllAsTouched();
      return;
    }

    const val = this.adjustmentForm.value;
    this.submitting.set(true);
    this.actionMessage.set(null);

    const amountPaise = Math.round(Number(val.amountRupees) * 100);
    const req: CreateTreasuryAdjustmentRequest = {
      transactionType: val.adjustmentType,
      transactionDate: val.transactionDate,
      bankAccountId: val.bankAccountId,
      nominalAccountId: val.contraLedgerAccountId,
      amountPaise: amountPaise,
      referenceNumber: val.referenceNumber || undefined,
      narration: val.description,
    };

    this.financeApi.createAdjustment(req).subscribe({
      next: (created) => {
        this.submitting.set(false);
        this.isModalOpen.set(false);
        this.actionMessage.set({
          type: 'ok',
          text: `Treasury adjustment "${created.transactionNumber}" recorded. Journal voucher posted.`,
        });
        this.loadTransactions();
        this.loadAccounts();
      },
      error: (err) => {
        this.submitting.set(false);
        this.actionMessage.set({
          type: 'error',
          text: err?.error?.error?.detail || 'Failed to post treasury adjustment.',
        });
      },
    });
  }

  protected formatPaise(paise: number): string {
    const rupees = (paise / 100).toLocaleString('en-IN', {
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    });
    return `₹${rupees}`;
  }
}
