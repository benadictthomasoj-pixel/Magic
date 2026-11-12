import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormArray, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import {
  AccountDto,
  CreateJournalDraftRequest,
  FiscalYearDto,
  JournalValidationResultDto,
} from '../../../core/contracts/accounting.models';
import { AccountingApiService } from '../../../core/services/accounting-api.service';
import { StatusBadgeComponent } from '../../../shared/components/status-badge/status-badge.component';

interface LineItemFormValue {
  accountId: string;
  narration: string;
  debit: number | null;
  credit: number | null;
}

@Component({
  selector: 'magic-journal-entry',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, StatusBadgeComponent],
  templateUrl: './journal-entry.component.html',
  styleUrl: './journal-entry.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JournalEntryComponent implements OnInit {
  private readonly api = inject(AccountingApiService);
  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);

  protected readonly accounts = signal<readonly AccountDto[]>([]);
  protected readonly fiscalYears = signal<readonly FiscalYearDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly isSubmitting = signal(false);

  protected readonly validationResult = signal<JournalValidationResultDto | null>(null);
  protected readonly actionMessage = signal<{ type: 'ok' | 'error' | 'warn'; text: string } | null>(null);

  protected readonly journalForm = this.fb.group({
    voucherType: ['Journal', [Validators.required]],
    voucherDate: [new Date().toISOString().split('T')[0], [Validators.required]],
    fiscalPeriodId: ['', [Validators.required]],
    referenceNumber: [''],
    narration: ['', [Validators.required, Validators.maxLength(1000)]],
    lines: this.fb.array([]),
  });

  get linesArray(): FormArray {
    return this.journalForm.get('lines') as FormArray;
  }

  ngOnInit(): void {
    this.loadPrerequisites();
  }

  private loadPrerequisites(): void {
    this.loading.set(true);

    this.api.getAccounts(undefined, true).subscribe({
      next: (accs) => this.accounts.set(accs),
      error: () => {},
    });

    this.api.getFiscalYears().subscribe({
      next: (fys) => {
        this.fiscalYears.set(fys);
        // Find current period
        const today = new Date().toISOString().split('T')[0];
        for (const fy of fys) {
          for (const p of fy.periods) {
            if (p.startDate <= today && p.endDate >= today) {
              this.journalForm.patchValue({ fiscalPeriodId: p.id });
              break;
            }
          }
        }
        if (!this.journalForm.value.fiscalPeriodId && fys.length > 0 && fys[0].periods.length > 0) {
          this.journalForm.patchValue({ fiscalPeriodId: fys[0].periods[0].id });
        }

        // Initialize 2 default draft lines
        if (this.linesArray.length === 0) {
          this.addLine();
          this.addLine();
        }

        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected addLine(): void {
    const defaultAccountId = this.accounts().length > 0 ? this.accounts()[0].id : '';
    const lineGroup = this.fb.group({
      accountId: [defaultAccountId, [Validators.required]],
      narration: [''],
      debit: [null as number | null, [Validators.min(0)]],
      credit: [null as number | null, [Validators.min(0)]],
    });

    this.linesArray.push(lineGroup);
    this.validationResult.set(null);
  }

  protected removeLine(index: number): void {
    if (this.linesArray.length > 2) {
      this.linesArray.removeAt(index);
      this.validationResult.set(null);
    }
  }

  protected onDebitChanged(index: number): void {
    const line = this.linesArray.at(index);
    const debitVal = line.get('debit')?.value;
    if (debitVal && debitVal > 0) {
      line.patchValue({ credit: null }, { emitEvent: false });
    }
    this.validationResult.set(null);
  }

  protected onCreditChanged(index: number): void {
    const line = this.linesArray.at(index);
    const creditVal = line.get('credit')?.value;
    if (creditVal && creditVal > 0) {
      line.patchValue({ debit: null }, { emitEvent: false });
    }
    this.validationResult.set(null);
  }

  protected calculateDebitTotal(): number {
    return this.linesArray.controls.reduce((sum, c) => sum + (Number(c.get('debit')?.value) || 0), 0);
  }

  protected calculateCreditTotal(): number {
    return this.linesArray.controls.reduce((sum, c) => sum + (Number(c.get('credit')?.value) || 0), 0);
  }

  protected calculateDifference(): number {
    return Math.abs(this.calculateDebitTotal() - this.calculateCreditTotal());
  }

  protected isBalanced(): boolean {
    const dr = this.calculateDebitTotal();
    const cr = this.calculateCreditTotal();
    return dr > 0 && Math.abs(dr - cr) < 0.001;
  }

  private buildRequest(): CreateJournalDraftRequest {
    const val = this.journalForm.getRawValue();
    const rawLines = val.lines as LineItemFormValue[];

    const lineInputs = rawLines.map((l) => ({
      accountId: l.accountId,
      debitPaise: Math.round((Number(l.debit) || 0) * 100),
      creditPaise: Math.round((Number(l.credit) || 0) * 100),
      narration: (l.narration || val.narration || '').trim(),
    }));

    return {
      fiscalPeriodId: val.fiscalPeriodId!,
      voucherType: val.voucherType!,
      voucherDate: val.voucherDate!,
      narration: (val.narration || '').trim(),
      referenceNumber: val.referenceNumber ? val.referenceNumber.trim() : null,
      lines: lineInputs,
    };
  }

  protected validateJournal(): void {
    const req = this.buildRequest();
    this.isSubmitting.set(true);
    this.actionMessage.set(null);

    this.api.validateJournal(req).subscribe({
      next: (res) => {
        this.isSubmitting.set(false);
        this.validationResult.set(res);
        if (res.isValid) {
          this.actionMessage.set({
            type: 'ok',
            text: 'Backend domain validator passed: All double-entry invariants and account statuses are verified.',
          });
        } else {
          this.actionMessage.set({
            type: 'warn',
            text: 'Validation errors detected. Resolve the listed issues before posting.',
          });
        }
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.actionMessage.set({
          type: 'error',
          text: err?.error?.error?.detail || 'Validation query failed.',
        });
      },
    });
  }

  protected saveDraft(): void {
    if (this.journalForm.invalid) {
      this.journalForm.markAllAsTouched();
      return;
    }

    const req = this.buildRequest();
    this.isSubmitting.set(true);
    this.actionMessage.set(null);

    this.api.createJournalDraft(req).subscribe({
      next: (voucher) => {
        this.isSubmitting.set(false);
        this.actionMessage.set({
          type: 'ok',
          text: `Draft voucher '${voucher.voucherNumber || voucher.id}' saved successfully.`,
        });
        setTimeout(() => this.router.navigate(['/accounting/vouchers']), 1200);
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.actionMessage.set({
          type: 'error',
          text: err?.error?.error?.detail || 'Failed to save draft voucher (Authentication required per OPEN-003).',
        });
      },
    });
  }

  protected postVoucher(): void {
    // Perform client & backend pre-check
    if (!this.isBalanced()) {
      this.actionMessage.set({
        type: 'error',
        text: 'Cannot post an unbalanced voucher. Total debits must strictly equal total credits.',
      });
      return;
    }

    this.isSubmitting.set(true);
    this.actionMessage.set(null);
    const req = this.buildRequest();

    // Draft first then post
    this.api.createJournalDraft(req).subscribe({
      next: (voucher) => {
        this.api.postVoucher(voucher.id, { branchCode: 'MAIN', fyCode: 'FY26' }).subscribe({
          next: (posted) => {
            this.isSubmitting.set(false);
            this.actionMessage.set({
              type: 'ok',
              text: `Voucher successfully posted to General Ledger: ${posted.voucherNumber}`,
            });
            setTimeout(() => this.router.navigate(['/accounting/vouchers']), 1500);
          },
          error: (err) => {
            this.isSubmitting.set(false);
            this.actionMessage.set({
              type: 'error',
              text: err?.error?.error?.detail || 'Posting rejected: Authenticated user identity required (OPEN-003).',
            });
          },
        });
      },
      error: (err) => {
        this.isSubmitting.set(false);
        this.actionMessage.set({
          type: 'error',
          text: err?.error?.error?.detail || 'Draft creation rejected by security boundary (OPEN-003).',
        });
      },
    });
  }

  protected formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-IN', {
      style: 'currency',
      currency: 'INR',
      minimumFractionDigits: 2,
    }).format(amount);
  }
}
