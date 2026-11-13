import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  ExpenseCategoryDto,
  ExpenseDto,
  ExpenseSummaryDto,
  PettyCashAccountDto,
  PettyCashTransactionDto,
} from '../../../core/contracts/operations.models';
import { OperationsApiService } from '../../../core/services/operations-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'magic-expenses-management',
  standalone: true,
  imports: [CommonModule, FormsModule, StatusBadgeComponent],
  templateUrl: './expenses-management.component.html',
  styleUrl: './expenses-management.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ExpensesManagementComponent implements OnInit {
  private readonly api = inject(OperationsApiService);

  protected readonly activeTab = signal<'expenses' | 'petty-cash' | 'categories'>('expenses');
  protected readonly summary = signal<ExpenseSummaryDto | null>(null);
  protected readonly expenses = signal<readonly ExpenseDto[]>([]);
  protected readonly pettyCashAccounts = signal<readonly PettyCashAccountDto[]>([]);
  protected readonly categories = signal<readonly ExpenseCategoryDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly showNewExpenseModal = signal(false);
  protected readonly showNewPettyCashModal = signal(false);

  // Filter state
  protected filterStatus = '';
  protected filterCategoryId = '';

  // Form state
  protected newExpense = {
    expenseCategoryId: '',
    description: '',
    amountRupees: 0,
    taxRupees: 0,
    paymentMethod: 'Cash',
    expenseDate: new Date().toISOString().split('T')[0],
  };

  protected newPettyCash = {
    accountName: '',
    floatLimitRupees: 5000,
    custodianUserId: '6df27c15-6264-45b4-b8b4-e8c7fafb1299',
    glAccountId: '11111111-1111-1111-1111-111111111111',
  };

  ngOnInit(): void {
    this.loadSummary();
    this.loadExpenses();
    this.loadCategories();
  }

  protected setTab(tab: 'expenses' | 'petty-cash' | 'categories'): void {
    this.activeTab.set(tab);
    if (tab === 'petty-cash') {
      this.loadPettyCash();
    }
  }

  protected loadSummary(): void {
    this.api.getExpenseSummary().subscribe({
      next: (res) => this.summary.set(res),
      error: () => {},
    });
  }

  protected loadExpenses(): void {
    this.loading.set(true);
    this.api
      .getExpenses({
        status: this.filterStatus || undefined,
        categoryId: this.filterCategoryId || undefined,
      })
      .subscribe({
        next: (res) => {
          this.expenses.set(res.items);
          this.loading.set(false);
        },
        error: () => this.loading.set(false),
      });
  }

  protected loadCategories(): void {
    this.api.getExpenseCategories().subscribe({
      next: (res) => {
        this.categories.set(res);
        if (res.length > 0 && !this.newExpense.expenseCategoryId) {
          this.newExpense.expenseCategoryId = res[0].id;
        }
      },
      error: () => {},
    });
  }

  protected loadPettyCash(): void {
    this.loading.set(true);
    this.api.getPettyCashAccounts().subscribe({
      next: (res) => {
        this.pettyCashAccounts.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected submitExpense(): void {
    if (!this.newExpense.description || this.newExpense.amountRupees <= 0) return;

    this.api
      .createExpense({
        expenseCategoryId: this.newExpense.expenseCategoryId,
        description: this.newExpense.description,
        amountPaise: Math.round(this.newExpense.amountRupees * 100),
        taxAmountPaise: Math.round(this.newExpense.taxRupees * 100),
        paymentMethod: this.newExpense.paymentMethod,
        expenseDate: this.newExpense.expenseDate,
      })
      .subscribe({
        next: () => {
          this.showNewExpenseModal.set(false);
          this.loadExpenses();
          this.loadSummary();
        },
      });
  }

  protected approveExpense(id: string): void {
    this.api.approveExpense(id).subscribe({
      next: () => {
        this.loadExpenses();
        this.loadSummary();
      },
    });
  }

  protected rejectExpense(id: string): void {
    const reason = prompt('Please enter rejection reason:');
    if (!reason) return;
    this.api.rejectExpense(id, reason).subscribe({
      next: () => {
        this.loadExpenses();
        this.loadSummary();
      },
    });
  }

  protected payExpense(id: string): void {
    const today = new Date().toISOString().split('T')[0];
    this.api
      .payExpense(id, {
        paymentMethod: 'Cash',
        paymentDate: today,
        referenceNumber: 'CASH-PAY',
      })
      .subscribe({
        next: () => {
          this.loadExpenses();
          this.loadSummary();
        },
      });
  }

  protected submitPettyCash(): void {
    if (!this.newPettyCash.accountName) return;
    this.api
      .createPettyCashAccount({
        accountName: this.newPettyCash.accountName,
        glAccountId: this.newPettyCash.glAccountId,
        custodianUserId: this.newPettyCash.custodianUserId,
        floatLimitPaise: Math.round(this.newPettyCash.floatLimitRupees * 100),
      })
      .subscribe({
        next: () => {
          this.showNewPettyCashModal.set(false);
          this.loadPettyCash();
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

