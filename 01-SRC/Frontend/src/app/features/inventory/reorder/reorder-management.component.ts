import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  ReorderRuleDto,
  ReorderSuggestionDto,
  UpsertReorderRuleRequest,
  WarehouseDto,
} from '../../../core/contracts/inventory.models';
import { InventoryApiService } from '../../../core/services/inventory-api.service';

@Component({
  selector: 'magic-reorder-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './reorder-management.component.html',
  styleUrl: './reorder-management.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReorderManagementComponent implements OnInit {
  private readonly api = inject(InventoryApiService);

  protected readonly activeTab = signal<'suggestions' | 'rules'>('suggestions');
  protected readonly suggestions = signal<readonly ReorderSuggestionDto[]>([]);
  protected readonly rules = signal<readonly ReorderRuleDto[]>([]);
  protected readonly warehouses = signal<readonly WarehouseDto[]>([]);
  protected readonly isCreateRuleOpen = signal<boolean>(false);
  protected readonly loading = signal<boolean>(true);
  protected readonly saving = signal<boolean>(false);
  protected readonly actionMessage = signal<{ type: 'ok' | 'error'; text: string } | null>(null);

  protected selectedWarehouseId = '';

  // Rule Form
  protected ruleForm: {
    warehouseId: string;
    productId: string;
    minStockLevel: number;
    maxStockLevel: number;
    reorderPoint: number;
    safetyStock: number;
    leadTimeDays: number;
    reorderQuantity: number;
    isActive: boolean;
  } = this.emptyRuleForm();

  ngOnInit(): void {
    this.loadWarehouses();
  }

  protected switchTab(tab: 'suggestions' | 'rules'): void {
    this.activeTab.set(tab);
    if (tab === 'suggestions') {
      this.loadSuggestions();
    } else {
      this.loadRules();
    }
  }

  protected loadWarehouses(): void {
    this.api.getWarehouses({ isActive: true, pageSize: 50 }).subscribe({
      next: (res) => {
        this.warehouses.set(res.items);
        if (res.items.length > 0 && !this.selectedWarehouseId) {
          this.selectedWarehouseId = res.items[0].id;
        }
        this.loadSuggestions();
      },
    });
  }

  protected loadSuggestions(): void {
    this.loading.set(true);
    this.api.getReorderSuggestions(this.selectedWarehouseId || undefined).subscribe({
      next: (sugs) => {
        this.suggestions.set(sugs);
        this.loading.set(false);
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to generate suggestions.' });
        this.loading.set(false);
      },
    });
  }

  protected loadRules(): void {
    this.loading.set(true);
    this.api.getReorderRules(this.selectedWarehouseId || undefined).subscribe({
      next: (rules) => {
        this.rules.set(rules);
        this.loading.set(false);
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to load rules.' });
        this.loading.set(false);
      },
    });
  }

  protected openCreateRule(): void {
    this.ruleForm = this.emptyRuleForm();
    if (this.selectedWarehouseId) {
      this.ruleForm.warehouseId = this.selectedWarehouseId;
    }
    this.isCreateRuleOpen.set(true);
  }

  protected saveRule(): void {
    if (!this.ruleForm.warehouseId || !this.ruleForm.productId) {
      alert('Please select a warehouse and specify product UUID.');
      return;
    }

    this.saving.set(true);
    const req: UpsertReorderRuleRequest = {
      warehouseId: this.ruleForm.warehouseId,
      productId: this.ruleForm.productId,
      minStockLevel: this.ruleForm.minStockLevel,
      maxStockLevel: this.ruleForm.maxStockLevel,
      reorderPoint: this.ruleForm.reorderPoint,
      safetyStock: this.ruleForm.safetyStock,
      leadTimeDays: this.ruleForm.leadTimeDays,
      reorderQuantity: this.ruleForm.reorderQuantity,
      isActive: this.ruleForm.isActive,
    };

    this.api.upsertReorderRule(req).subscribe({
      next: () => {
        this.actionMessage.set({ type: 'ok', text: 'Reorder rule configured successfully.' });
        this.saving.set(false);
        this.isCreateRuleOpen.set(false);
        if (this.activeTab() === 'rules') {
          this.loadRules();
        } else {
          this.loadSuggestions();
        }
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to save rule.' });
        this.saving.set(false);
      },
    });
  }

  protected closeDrawers(): void {
    this.isCreateRuleOpen.set(false);
  }

  private emptyRuleForm() {
    return {
      warehouseId: '',
      productId: '',
      minStockLevel: 10,
      maxStockLevel: 100,
      reorderPoint: 25,
      safetyStock: 10,
      leadTimeDays: 7,
      reorderQuantity: 50,
      isActive: true,
    };
  }
}

