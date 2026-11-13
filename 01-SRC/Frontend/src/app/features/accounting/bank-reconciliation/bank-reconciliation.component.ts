import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import {
  BankCashAccountDto,
  BankStatementLineDto,
  BankStatementSummaryDto,
  ImportBankStatementRequest,
  ReconciliationWorkspaceDto,
  UnmatchedLedgerTransactionDto,
} from '../../../core/contracts/finance.models';
import { FinanceApiService } from '../../../core/services/finance-api.service';

@Component({
  selector: 'magic-bank-reconciliation',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './bank-reconciliation.component.html',
  styleUrl: './bank-reconciliation.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BankReconciliationComponent implements OnInit {
  private readonly financeApi = inject(FinanceApiService);
  private readonly fb = inject(FormBuilder);

  protected readonly bankAccounts = signal<readonly BankCashAccountDto[]>([]);
  protected readonly statements = signal<readonly BankStatementSummaryDto[]>([]);
  protected readonly currentWorkspace = signal<ReconciliationWorkspaceDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly processing = signal(false);
  protected readonly isImportModalOpen = signal(false);
  protected readonly actionMessage = signal<{ type: 'ok' | 'error'; text: string } | null>(null);

  protected selectedBankAccountId = '';
  protected selectedStatementLine = signal<BankStatementLineDto | null>(null);
  protected selectedGlLine = signal<UnmatchedLedgerTransactionDto | null>(null);

  protected importForm!: FormGroup;

  ngOnInit(): void {
    this.initForm();
    this.loadBankAccounts();
  }

  private initForm(): void {
    const today = new Date().toISOString().slice(0, 10);
    this.importForm = this.fb.group({
      bankAccountId: ['', [Validators.required]],
      statementStartDate: [today, [Validators.required]],
      statementEndDate: [today, [Validators.required]],
      openingBalanceRupees: [0, [Validators.required]],
      closingBalanceRupees: [0, [Validators.required]],
      filename: ['bank_statement.csv', [Validators.required]],
      csvContent: ['', [Validators.required]],
    });
  }

  protected loadBankAccounts(): void {
    this.financeApi.getBankAccounts('Bank', true).subscribe({
      next: (banks) => {
        this.bankAccounts.set(banks);
        if (banks.length > 0 && !this.selectedBankAccountId) {
          this.selectedBankAccountId = banks[0].id;
          this.loadStatements();
        } else {
          this.loading.set(false);
        }
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadStatements(): void {
    if (!this.selectedBankAccountId) return;
    this.loading.set(true);
    this.currentWorkspace.set(null);

    this.financeApi.getReconciliationStatements(this.selectedBankAccountId).subscribe({
      next: (list) => {
        this.statements.set(list);
        this.loading.set(false);
      },
      error: (err) => {
        this.actionMessage.set({
          type: 'error',
          text: err?.error?.error?.detail || 'Failed to load bank reconciliation statements.',
        });
        this.loading.set(false);
      },
    });
  }

  protected openWorkspace(statementId: string): void {
    this.loading.set(true);
    this.selectedStatementLine.set(null);
    this.selectedGlLine.set(null);

    this.financeApi.getReconciliationWorkspace(statementId).subscribe({
      next: (workspace) => {
        this.currentWorkspace.set(workspace);
        this.loading.set(false);
      },
      error: (err) => {
        this.actionMessage.set({
          type: 'error',
          text: err?.error?.error?.detail || 'Failed to load reconciliation workspace.',
        });
        this.loading.set(false);
      },
    });
  }

  protected closeWorkspace(): void {
    this.currentWorkspace.set(null);
    this.selectedStatementLine.set(null);
    this.selectedGlLine.set(null);
    this.loadStatements();
  }

  protected openImportModal(): void {
    const today = new Date().toISOString().slice(0, 10);
    this.importForm.reset({
      bankAccountId: this.selectedBankAccountId,
      statementStartDate: today,
      statementEndDate: today,
      openingBalanceRupees: 0,
      closingBalanceRupees: 0,
      filename: 'bank_statement.csv',
      csvContent: `Date,Description,Reference,Debit,Credit,Balance
2026-11-10,Customer NEFT payment,NEFT-001,0,50000.00,50000.00
2026-11-11,Supplier RTGS payment,RTGS-002,15000.00,0,35000.00
2026-11-12,Bank Charges,CHG-NOV,250.00,0,34750.00`,
    });
    this.isImportModalOpen.set(true);
  }

  protected closeImportModal(): void {
    this.isImportModalOpen.set(false);
  }

  protected submitImport(): void {
    if (this.importForm.invalid) {
      this.importForm.markAllAsTouched();
      return;
    }

    const val = this.importForm.value;
    this.processing.set(true);
    this.actionMessage.set(null);

    const openPaise = Math.round(Number(val.openingBalanceRupees) * 100);
    const closingPaise = Math.round(Number(val.closingBalanceRupees) * 100);
    const req: ImportBankStatementRequest = {
      bankAccountId: val.bankAccountId,
      statementStartDate: val.statementStartDate,
      statementEndDate: val.statementEndDate,
      openingBalancePaise: openPaise,
      closingBalancePaise: closingPaise,
      filename: val.filename,
      csvContent: val.csvContent,
    };

    this.financeApi.importStatementCsv(req).subscribe({
      next: (imported) => {
        this.processing.set(false);
        this.isImportModalOpen.set(false);
        this.actionMessage.set({
          type: 'ok',
          text: `Bank statement imported successfully with ${imported.lines.length} statement lines.`,
        });
        this.openWorkspace(imported.id);
      },
      error: (err) => {
        this.processing.set(false);
        this.actionMessage.set({
          type: 'error',
          text: err?.error?.error?.detail || 'Failed to import CSV statement.',
        });
      },
    });
  }

  protected selectStatementLine(line: BankStatementLineDto): void {
    if (line.isReconciled) return;
    this.selectedStatementLine.set(line);
  }

  protected selectGlLine(gl: UnmatchedLedgerTransactionDto): void {
    this.selectedGlLine.set(gl);
  }

  protected matchLines(): void {
    const ws = this.currentWorkspace();
    const stLine = this.selectedStatementLine();
    const glLine = this.selectedGlLine();

    if (!ws || !stLine || !glLine) return;

    this.processing.set(true);
    this.financeApi
      .matchStatementLine(ws.statementId, {
        statementLineId: stLine.id,
        journalLineId: glLine.journalLineId,
        matchType: 'Manual',
      })
      .subscribe({
        next: (updatedWs) => {
          this.processing.set(false);
          this.currentWorkspace.set(updatedWs);
          this.selectedStatementLine.set(null);
          this.selectedGlLine.set(null);
          this.actionMessage.set({
            type: 'ok',
            text: 'Statement line matched with GL line.',
          });
        },
        error: (err) => {
          this.processing.set(false);
          this.actionMessage.set({
            type: 'error',
            text: err?.error?.error?.detail || 'Failed to match line.',
          });
        },
      });
  }

  protected unmatchLine(line: BankStatementLineDto): void {
    const ws = this.currentWorkspace();
    if (!ws) return;

    this.processing.set(true);
    this.financeApi
      .unmatchStatementLine(ws.statementId, {
        statementLineId: line.id,
      })
      .subscribe({
        next: (updatedWs) => {
          this.processing.set(false);
          this.currentWorkspace.set(updatedWs);
          this.actionMessage.set({
            type: 'ok',
            text: 'Statement line un-matched and returned to open reconciliation pool.',
          });
        },
        error: (err) => {
          this.processing.set(false);
          this.actionMessage.set({
            type: 'error',
            text: err?.error?.error?.detail || 'Failed to unmatch line.',
          });
        },
      });
  }

  protected completeReconciliation(): void {
    const ws = this.currentWorkspace();
    if (!ws) return;

    if (ws.differencePaise !== 0) {
      if (!confirm(`Warning: There is an unreconciled difference of ${this.formatPaise(ws.differencePaise)}. Complete reconciliation anyway?`)) {
        return;
      }
    }

    this.processing.set(true);
    this.financeApi.completeReconciliation(ws.statementId).subscribe({
      next: () => {
        this.processing.set(false);
        this.actionMessage.set({
          type: 'ok',
          text: 'Bank reconciliation completed and signed off successfully.',
        });
        this.closeWorkspace();
      },
      error: (err) => {
        this.processing.set(false);
        this.actionMessage.set({
          type: 'error',
          text: err?.error?.error?.detail || 'Failed to complete reconciliation.',
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
