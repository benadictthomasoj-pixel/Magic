import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  EnterpriseApiService,
  CompanyProfile,
  Branch,
  TerminalDetails,
  InterBranchTransfer,
} from '../../../core/services/enterprise-api.service';

@Component({
  selector: 'magic-branch-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './branch-management.component.html',
  styleUrl: './branch-management.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BranchManagementComponent implements OnInit {
  private readonly api = inject(EnterpriseApiService);

  protected readonly activeTab = signal<'branches' | 'terminals' | 'transfers' | 'company'>('branches');
  protected readonly company = signal<CompanyProfile | null>(null);
  protected readonly branches = signal<readonly Branch[]>([]);
  protected readonly terminals = signal<readonly TerminalDetails[]>([]);
  protected readonly transfers = signal<readonly InterBranchTransfer[]>([]);
  protected readonly loading = signal(false);
  protected readonly actionMessage = signal<string | null>(null);

  // Modals
  protected readonly showBranchModal = signal(false);
  protected readonly showTerminalModal = signal(false);
  protected readonly showTransferModal = signal(false);

  // New Branch Form State
  protected branchForm = {
    branchCode: '',
    branchName: '',
    address: '',
    phone: '',
    email: '',
    gstin: '',
    branchManager: '',
  };

  // New Terminal Form State
  protected terminalForm = {
    branchId: '',
    terminalCode: '',
    machineName: '',
    operatingSystem: 'macOS Apple Silicon',
    assignedCashierName: '',
    posPrinterModel: 'Generic 80mm Thermal',
    receiptWidthMm: 80,
  };

  // New Transfer Form State
  protected transferForm = {
    sourceBranchId: '',
    sourceWarehouseId: '',
    destinationBranchId: '',
    destinationWarehouseId: '',
    notes: '',
    productId: '',
    quantity: 1,
  };

  ngOnInit(): void {
    this.loadBranches();
    this.loadCompany();
  }

  protected setTab(tab: 'branches' | 'terminals' | 'transfers' | 'company'): void {
    this.activeTab.set(tab);
    if (tab === 'branches') this.loadBranches();
    if (tab === 'terminals') this.loadTerminals();
    if (tab === 'transfers') this.loadTransfers();
    if (tab === 'company') this.loadCompany();
  }

  protected loadCompany(): void {
    this.api.getCompanyProfile().subscribe({
      next: (res) => this.company.set(res),
      error: () => {},
    });
  }

  protected loadBranches(): void {
    this.loading.set(true);
    this.api.getBranches().subscribe({
      next: (res) => {
        this.branches.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadTerminals(): void {
    this.loading.set(true);
    this.api.getTerminals().subscribe({
      next: (res) => {
        this.terminals.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected loadTransfers(): void {
    this.loading.set(true);
    this.api.getInterBranchTransfers().subscribe({
      next: (res) => {
        this.transfers.set(res);
        this.loading.set(false);
      },
      error: () => this.loading.set(false),
    });
  }

  protected createBranch(): void {
    this.api.createBranch(this.branchForm).subscribe({
      next: () => {
        this.showBranchModal.set(false);
        this.notify('Branch created successfully');
        this.loadBranches();
      },
      error: (err) => this.notify(`Error: ${err.message || 'Failed to create branch'}`),
    });
  }

  protected registerTerminal(): void {
    this.api.registerTerminal(this.terminalForm).subscribe({
      next: () => {
        this.showTerminalModal.set(false);
        this.notify('Terminal registered and authorized successfully');
        this.loadTerminals();
      },
      error: (err) => this.notify(`Error: ${err.message || 'Failed to register terminal'}`),
    });
  }

  protected authorizeTerminal(terminalId: string, authorize: boolean): void {
    this.api.authorizeTerminal(terminalId, authorize).subscribe({
      next: () => {
        this.notify(`Terminal authorization status updated`);
        this.loadTerminals();
      },
      error: (err) => this.notify(`Error: ${err.message || 'Update failed'}`),
    });
  }

  protected requestTransfer(): void {
    this.api
      .requestInterBranchTransfer({
        sourceBranchId: this.transferForm.sourceBranchId,
        sourceWarehouseId: this.transferForm.sourceWarehouseId,
        destinationBranchId: this.transferForm.destinationBranchId,
        destinationWarehouseId: this.transferForm.destinationWarehouseId,
        notes: this.transferForm.notes,
        lines: [
          {
            productId: this.transferForm.productId,
            requestedQuantity: this.transferForm.quantity,
          },
        ],
      })
      .subscribe({
        next: () => {
          this.showTransferModal.set(false);
          this.notify('Stock transfer request submitted');
          this.loadTransfers();
        },
        error: (err) => this.notify(`Error: ${err.message || 'Transfer request failed'}`),
      });
  }

  protected approveTransfer(id: string): void {
    this.api.approveInterBranchTransfer(id).subscribe({
      next: () => {
        this.notify('Transfer approved');
        this.loadTransfers();
      },
      error: (err) => this.notify(`Approval failed: ${err.message}`),
    });
  }

  protected dispatchTransfer(id: string): void {
    this.api.dispatchInterBranchTransfer(id).subscribe({
      next: () => {
        this.notify('Transfer dispatched - transit recorded');
        this.loadTransfers();
      },
      error: (err) => this.notify(`Dispatch failed: ${err.message}`),
    });
  }

  protected receiveTransfer(id: string): void {
    this.api.receiveInterBranchTransfer(id).subscribe({
      next: () => {
        this.notify('Transfer goods received - inventory ledger updated');
        this.loadTransfers();
      },
      error: (err) => this.notify(`Receive failed: ${err.message}`),
    });
  }

  private notify(msg: string): void {
    this.actionMessage.set(msg);
    setTimeout(() => this.actionMessage.set(null), 4000);
  }
}
