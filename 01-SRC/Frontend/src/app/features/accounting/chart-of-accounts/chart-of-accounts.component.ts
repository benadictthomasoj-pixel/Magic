import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule, ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { AccountDto, AccountGroupDto, CreateAccountRequest } from '../../../core/contracts/accounting.models';
import { AccountingApiService } from '../../../core/services/accounting-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

@Component({
  selector: 'magic-chart-of-accounts',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, StatusBadgeComponent],
  templateUrl: './chart-of-accounts.component.html',
  styleUrl: './chart-of-accounts.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChartOfAccountsComponent implements OnInit {
  private readonly api = inject(AccountingApiService);
  private readonly fb = inject(FormBuilder);

  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly accounts = signal<readonly AccountDto[]>([]);
  protected readonly groups = signal<readonly AccountGroupDto[]>([]);

  protected readonly selectedRootClass = signal<string>('All');
  protected readonly searchQuery = signal<string>('');
  protected readonly activeOnly = signal<boolean>(true);

  protected readonly isCreateModalOpen = signal(false);
  protected readonly isSubmitting = signal(false);
  protected readonly formError = signal<string | null>(null);

  protected readonly createForm = this.fb.group({
    code: ['', [Validators.required, Validators.maxLength(30), Validators.pattern(/^[A-Za-z0-9\-_.]+$/)]],
    name: ['', [Validators.required, Validators.maxLength(150)]],
    rootClass: ['Asset', [Validators.required]],
    groupId: ['', [Validators.required]],
    controlAccountType: [''],
  });

  ngOnInit(): void {
    this.loadData();
  }

  protected loadData(): void {
    this.loading.set(true);
    this.error.set(null);

    this.api.getAccountGroups().subscribe({
      next: (groups) => this.groups.set(groups),
      error: () => {},
    });

    const rootFilter = this.selectedRootClass() === 'All' ? undefined : this.selectedRootClass();

    this.api.getAccounts(rootFilter, this.activeOnly(), this.searchQuery()).subscribe({
      next: (accs) => {
        this.accounts.set(accs);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.error?.detail || 'Failed to retrieve Chart of Accounts.');
        this.loading.set(false);
      },
    });
  }

  protected setFilter(rootClass: string): void {
    this.selectedRootClass.set(rootClass);
    this.loadData();
  }

  protected onSearchChange(query: string): void {
    this.searchQuery.set(query);
    this.loadData();
  }

  protected toggleActiveOnly(): void {
    this.activeOnly.update((v) => !v);
    this.loadData();
  }

  protected openCreateModal(): void {
    this.formError.set(null);
    this.createForm.reset({
      code: '',
      name: '',
      rootClass: 'Asset',
      groupId: this.groups().find((g) => g.rootClass === 'Asset')?.id || '',
      controlAccountType: '',
    });
    this.isCreateModalOpen.set(true);
  }

  protected closeCreateModal(): void {
    this.isCreateModalOpen.set(false);
  }

  protected onRootClassChanged(newClass: string): void {
    const matchingGroup = this.groups().find((g) => g.rootClass === newClass);
    if (matchingGroup) {
      this.createForm.patchValue({ groupId: matchingGroup.id });
    }
  }

  protected submitCreateAccount(): void {
    if (this.createForm.invalid) {
      this.createForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.formError.set(null);

    const val = this.createForm.getRawValue();
    const req: CreateAccountRequest = {
      code: val.code!.trim(),
      name: val.name!.trim(),
      rootClass: val.rootClass!,
      groupId: val.groupId!,
      controlAccountType: val.controlAccountType ? val.controlAccountType : null,
    };

    this.api.createAccount(req).subscribe({
      next: () => {
        this.isSubmitting.set(false);
        this.isCreateModalOpen.set(false);
        this.loadData();
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.formError.set(err?.error?.error?.detail || err?.error?.error?.title || 'Account creation rejected.');
      },
    });
  }

  protected formatPaise(paise: number): string {
    const rupees = paise / 100;
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      minimumFractionDigits: 2,
    }).format(rupees);
  }
}
