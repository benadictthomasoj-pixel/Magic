import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  CreateStorageLocationRequest,
  CreateWarehouseRequest,
  StorageLocationDto,
  UpdateWarehouseRequest,
  WarehouseDto,
} from '../../../core/contracts/inventory.models';
import { InventoryApiService } from '../../../core/services/inventory-api.service';

@Component({
  selector: 'magic-warehouses-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './warehouses-list.component.html',
  styleUrl: './warehouses-list.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WarehousesListComponent implements OnInit {
  private readonly api = inject(InventoryApiService);

  protected readonly warehouses = signal<readonly WarehouseDto[]>([]);
  protected readonly selectedWarehouse = signal<WarehouseDto | null>(null);
  protected readonly locations = signal<readonly StorageLocationDto[]>([]);
  protected readonly isCreateOpen = signal<boolean>(false);
  protected readonly isEditOpen = signal<boolean>(false);
  protected readonly isLocationsOpen = signal<boolean>(false);
  protected readonly loading = signal<boolean>(true);
  protected readonly saving = signal<boolean>(false);
  protected readonly actionMessage = signal<{ type: 'ok' | 'error'; text: string } | null>(null);

  protected filterSearch = '';
  protected filterActive: boolean | undefined = undefined;
  protected currentPage = 1;
  protected pageSize = 20;
  protected totalWarehouses = 0;

  // Warehouse Form Model
  protected formModel: {
    id?: string;
    code: string;
    name: string;
    description: string;
    addressLine1: string;
    addressLine2: string;
    city: string;
    state: string;
    stateCode: string;
    pinCode: string;
    country: string;
    phone: string;
    email: string;
    managerName: string;
    capacitySqFt: number | null;
    isActive: boolean;
  } = this.emptyForm();

  // Location Form Model
  protected locationForm: {
    code: string;
    name: string;
    locationType: string;
    aisle: string;
    rack: string;
    shelf: string;
    bin: string;
  } = this.emptyLocationForm();

  ngOnInit(): void {
    this.loadWarehouses();
  }

  protected loadWarehouses(): void {
    this.loading.set(true);
    this.api
      .getWarehouses({
        searchTerm: this.filterSearch.trim() || undefined,
        isActive: this.filterActive,
        page: this.currentPage,
        pageSize: this.pageSize,
      })
      .subscribe({
        next: (res) => {
          this.warehouses.set(res.items);
          this.totalWarehouses = res.totalCount;
          this.loading.set(false);
        },
        error: (err) => {
          this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to load warehouses.' });
          this.loading.set(false);
        },
      });
  }

  protected onSearchChange(): void {
    this.currentPage = 1;
    this.loadWarehouses();
  }

  protected openCreate(): void {
    this.formModel = this.emptyForm();
    this.isCreateOpen.set(true);
  }

  protected openEdit(wh: WarehouseDto): void {
    this.formModel = {
      id: wh.id,
      code: wh.code,
      name: wh.name,
      description: wh.description || '',
      addressLine1: wh.addressLine1,
      addressLine2: wh.addressLine2 || '',
      city: wh.city,
      state: wh.state,
      stateCode: wh.stateCode,
      pinCode: wh.pinCode,
      country: wh.country,
      phone: wh.phone || '',
      email: wh.email || '',
      managerName: wh.managerName || '',
      capacitySqFt: wh.capacitySqFt ?? null,
      isActive: wh.isActive,
    };
    this.isEditOpen.set(true);
  }

  protected closeDrawers(): void {
    this.isCreateOpen.set(false);
    this.isEditOpen.set(false);
    this.isLocationsOpen.set(false);
  }

  protected saveWarehouse(): void {
    if (!this.formModel.code || !this.formModel.name || !this.formModel.addressLine1 || !this.formModel.city || !this.formModel.state || !this.formModel.pinCode) {
      this.actionMessage.set({ type: 'error', text: 'Please fill in all mandatory fields (*).' });
      return;
    }

    this.saving.set(true);
    if (this.formModel.id) {
      const updateReq: UpdateWarehouseRequest = {
        name: this.formModel.name,
        description: this.formModel.description || null,
        addressLine1: this.formModel.addressLine1,
        addressLine2: this.formModel.addressLine2 || null,
        city: this.formModel.city,
        state: this.formModel.state,
        stateCode: this.formModel.stateCode,
        pinCode: this.formModel.pinCode,
        country: this.formModel.country,
        phone: this.formModel.phone || null,
        email: this.formModel.email || null,
        managerName: this.formModel.managerName || null,
        capacitySqFt: this.formModel.capacitySqFt,
        isActive: this.formModel.isActive,
      };

      this.api.updateWarehouse(this.formModel.id, updateReq).subscribe({
        next: () => {
          this.actionMessage.set({ type: 'ok', text: `Warehouse "${this.formModel.name}" updated successfully.` });
          this.saving.set(false);
          this.closeDrawers();
          this.loadWarehouses();
        },
        error: (err) => {
          this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to update warehouse.' });
          this.saving.set(false);
        },
      });
    } else {
      const createReq: CreateWarehouseRequest = {
        code: this.formModel.code.toUpperCase(),
        name: this.formModel.name,
        description: this.formModel.description || null,
        addressLine1: this.formModel.addressLine1,
        addressLine2: this.formModel.addressLine2 || null,
        city: this.formModel.city,
        state: this.formModel.state,
        stateCode: this.formModel.stateCode,
        pinCode: this.formModel.pinCode,
        country: this.formModel.country,
        phone: this.formModel.phone || null,
        email: this.formModel.email || null,
        managerName: this.formModel.managerName || null,
        capacitySqFt: this.formModel.capacitySqFt,
      };

      this.api.createWarehouse(createReq).subscribe({
        next: (created) => {
          this.actionMessage.set({ type: 'ok', text: `Warehouse "${created.name}" created successfully.` });
          this.saving.set(false);
          this.closeDrawers();
          this.loadWarehouses();
        },
        error: (err) => {
          this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to create warehouse.' });
          this.saving.set(false);
        },
      });
    }
  }

  protected archiveWarehouse(wh: WarehouseDto): void {
    if (!confirm(`Are you sure you want to deactivate/archive warehouse "${wh.name}"?`)) return;

    this.api.archiveWarehouse(wh.id).subscribe({
      next: () => {
        this.actionMessage.set({ type: 'ok', text: `Warehouse "${wh.name}" deactivated.` });
        this.loadWarehouses();
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to archive warehouse.' });
      },
    });
  }

  protected openLocations(wh: WarehouseDto): void {
    this.selectedWarehouse.set(wh);
    this.locationForm = this.emptyLocationForm();
    this.isLocationsOpen.set(true);
    this.loadLocations(wh.id);
  }

  protected loadLocations(warehouseId: string): void {
    this.api.getStorageLocations(warehouseId).subscribe({
      next: (locs) => this.locations.set(locs),
      error: (err) => this.actionMessage.set({ type: 'error', text: err?.error?.detail || 'Failed to load locations.' }),
    });
  }

  protected saveLocation(): void {
    const wh = this.selectedWarehouse();
    if (!wh) return;

    if (!this.locationForm.code || !this.locationForm.name) {
      alert('Location Code and Name are required.');
      return;
    }

    const req: CreateStorageLocationRequest = {
      code: this.locationForm.code.toUpperCase(),
      name: this.locationForm.name,
      locationType: this.locationForm.locationType,
      aisle: this.locationForm.aisle || null,
      rack: this.locationForm.rack || null,
      shelf: this.locationForm.shelf || null,
      bin: this.locationForm.bin || null,
    };

    this.api.createStorageLocation(wh.id, req).subscribe({
      next: () => {
        this.locationForm = this.emptyLocationForm();
        this.loadLocations(wh.id);
      },
      error: (err) => alert(err?.error?.detail || 'Failed to create location.'),
    });
  }

  private emptyForm() {
    return {
      code: '',
      name: '',
      description: '',
      addressLine1: '',
      addressLine2: '',
      city: '',
      state: 'Tamil Nadu',
      stateCode: '33',
      pinCode: '',
      country: 'India',
      phone: '',
      email: '',
      managerName: '',
      capacitySqFt: null,
      isActive: true,
    };
  }

  private emptyLocationForm() {
    return {
      code: '',
      name: '',
      locationType: 'Bin',
      aisle: '',
      rack: '',
      shelf: '',
      bin: '',
    };
  }
}
