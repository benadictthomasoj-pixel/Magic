import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  BrandDto,
  CategoryDto,
  CreateBrandRequest,
  CreateCategoryRequest,
  CreateDepartmentRequest,
  DepartmentDto,
  UpdateCategoryRequest,
} from '../../../core/contracts/commercial.models';
import { CommercialApiService } from '../../../core/services/commercial-api.service';

@Component({
  selector: 'magic-categories-management',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './categories-management.component.html',
  styleUrl: './categories-management.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CategoriesManagementComponent implements OnInit {
  private readonly api = inject(CommercialApiService);

  protected readonly activeTab = signal<'categories' | 'brands' | 'departments'>('categories');
  protected readonly categories = signal<readonly CategoryDto[]>([]);
  protected readonly brands = signal<readonly BrandDto[]>([]);
  protected readonly departments = signal<readonly DepartmentDto[]>([]);

  protected readonly loading = signal<boolean>(true);
  protected readonly saving = signal<boolean>(false);
  protected readonly actionMessage = signal<{ type: 'ok' | 'error'; text: string } | null>(null);

  // Category Drawers
  protected readonly isCategoryDrawerOpen = signal<boolean>(false);
  protected readonly isEditingCategory = signal<boolean>(false);
  protected categoryForm: {
    id?: string;
    code: string;
    name: string;
    description: string;
    parentCategoryId: string;
    departmentId: string;
    hsnCode: string;
    defaultGstRate: number;
    displayOrder: number;
  } = this.getEmptyCategoryForm();

  // Brand Modal
  protected readonly isBrandModalOpen = signal<boolean>(false);
  protected brandForm: {
    code: string;
    name: string;
    description: string;
    websiteUrl: string;
  } = { code: '', name: '', description: '', websiteUrl: '' };

  // Department Modal
  protected readonly isDeptModalOpen = signal<boolean>(false);
  protected deptForm: {
    code: string;
    name: string;
    description: string;
  } = { code: '', name: '', description: '' };

  ngOnInit(): void {
    this.loadAll();
  }

  protected loadAll(): void {
    this.loading.set(true);
    this.api.getCategoriesTree().subscribe({
      next: (cats) => {
        this.categories.set(cats);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        this.actionMessage.set({
          type: 'error',
          text: err.error?.detail || 'Failed to load categories.',
        });
      },
    });

    this.api.getBrands().subscribe({
      next: (b) => this.brands.set(b),
      error: () => {},
    });

    this.api.getDepartments().subscribe({
      next: (d) => this.departments.set(d),
      error: () => {},
    });
  }

  // --- Category Actions ---
  protected openCreateCategory(): void {
    this.categoryForm = this.getEmptyCategoryForm();
    this.isEditingCategory.set(false);
    this.isCategoryDrawerOpen.set(true);
  }

  protected openEditCategory(cat: CategoryDto): void {
    this.categoryForm = {
      id: cat.id,
      code: cat.code,
      name: cat.name,
      description: cat.description || '',
      parentCategoryId: cat.parentCategoryId || '',
      departmentId: cat.departmentId || '',
      hsnCode: cat.hsnCode || '',
      defaultGstRate: cat.defaultGstRate || 18,
      displayOrder: cat.displayOrder,
    };
    this.isEditingCategory.set(true);
    this.isCategoryDrawerOpen.set(true);
  }

  protected closeCategoryDrawer(): void {
    this.isCategoryDrawerOpen.set(false);
  }

  protected saveCategory(): void {
    if (!this.categoryForm.name.trim() || !this.categoryForm.code.trim()) {
      this.actionMessage.set({ type: 'error', text: 'Category code and name are required.' });
      return;
    }

    // Circular hierarchy prevention
    if (this.isEditingCategory() && this.categoryForm.id) {
      if (this.categoryForm.parentCategoryId === this.categoryForm.id) {
        this.actionMessage.set({
          type: 'error',
          text: 'Circular reference error: A category cannot be its own parent.',
        });
        return;
      }
    }

    this.saving.set(true);
    this.actionMessage.set(null);

    if (this.isEditingCategory() && this.categoryForm.id) {
      const updateReq: UpdateCategoryRequest = {
        name: this.categoryForm.name.trim(),
        description: this.categoryForm.description.trim() || null,
        parentCategoryId: this.categoryForm.parentCategoryId || null,
        departmentId: this.categoryForm.departmentId || null,
        hsnCode: this.categoryForm.hsnCode.trim() || null,
        defaultGstRate: this.categoryForm.defaultGstRate,
        displayOrder: this.categoryForm.displayOrder,
        imageUrl: null,
        isActive: true,
      };

      this.api.updateCategory(this.categoryForm.id, updateReq).subscribe({
        next: (saved) => {
          this.saving.set(false);
          this.closeCategoryDrawer();
          this.actionMessage.set({
            type: 'ok',
            text: `Category "${saved.name}" updated successfully.`,
          });
          this.loadAll();
        },
        error: (err) => {
          this.saving.set(false);
          this.actionMessage.set({
            type: 'error',
            text: err.error?.detail || 'Failed to update category.',
          });
        },
      });
    } else {
      const createReq: CreateCategoryRequest = {
        code: this.categoryForm.code.trim().toUpperCase(),
        name: this.categoryForm.name.trim(),
        description: this.categoryForm.description.trim() || null,
        parentCategoryId: this.categoryForm.parentCategoryId || null,
        departmentId: this.categoryForm.departmentId || null,
        hsnCode: this.categoryForm.hsnCode.trim() || null,
        defaultGstRate: this.categoryForm.defaultGstRate,
        displayOrder: this.categoryForm.displayOrder,
        imageUrl: null,
      };

      this.api.createCategory(createReq).subscribe({
        next: (created) => {
          this.saving.set(false);
          this.closeCategoryDrawer();
          this.actionMessage.set({
            type: 'ok',
            text: `Category "${created.name}" (${created.code}) created successfully.`,
          });
          this.loadAll();
        },
        error: (err) => {
          this.saving.set(false);
          this.actionMessage.set({
            type: 'error',
            text: err.error?.detail || 'Failed to create category.',
          });
        },
      });
    }
  }

  protected deleteCategory(cat: CategoryDto): void {
    if (cat.productCount > 0) {
      this.actionMessage.set({
        type: 'error',
        text: `Cannot delete category "${cat.name}" because ${cat.productCount} active products are currently assigned to it. Reassign or delete products first.`,
      });
      return;
    }

    if (!confirm(`Are you sure you want to delete category "${cat.name}"?`)) {
      return;
    }

    this.api.deleteCategory(cat.id).subscribe({
      next: () => {
        this.actionMessage.set({
          type: 'ok',
          text: `Category "${cat.name}" deleted successfully.`,
        });
        this.loadAll();
      },
      error: (err) => {
        this.actionMessage.set({
          type: 'error',
          text: err.error?.detail || 'Failed to delete category.',
        });
      },
    });
  }

  // --- Brand Actions ---
  protected openCreateBrand(): void {
    this.brandForm = { code: '', name: '', description: '', websiteUrl: '' };
    this.isBrandModalOpen.set(true);
  }

  protected saveBrand(): void {
    if (!this.brandForm.code.trim() || !this.brandForm.name.trim()) {
      this.actionMessage.set({ type: 'error', text: 'Brand code and name are required.' });
      return;
    }

    const req: CreateBrandRequest = {
      code: this.brandForm.code.trim().toUpperCase(),
      name: this.brandForm.name.trim(),
      description: this.brandForm.description.trim() || null,
      logoUrl: null,
      websiteUrl: this.brandForm.websiteUrl.trim() || null,
    };

    this.api.createBrand(req).subscribe({
      next: (b) => {
        this.isBrandModalOpen.set(false);
        this.actionMessage.set({ type: 'ok', text: `Brand "${b.name}" registered.` });
        this.api.getBrands().subscribe((res) => this.brands.set(res));
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err.error?.detail || 'Failed to register brand.' });
      },
    });
  }

  // --- Department Actions ---
  protected openCreateDepartment(): void {
    this.deptForm = { code: '', name: '', description: '' };
    this.isDeptModalOpen.set(true);
  }

  protected saveDepartment(): void {
    if (!this.deptForm.code.trim() || !this.deptForm.name.trim()) {
      this.actionMessage.set({ type: 'error', text: 'Department code and name are required.' });
      return;
    }

    const req: CreateDepartmentRequest = {
      code: this.deptForm.code.trim().toUpperCase(),
      name: this.deptForm.name.trim(),
      description: this.deptForm.description.trim() || null,
    };

    this.api.createDepartment(req).subscribe({
      next: (d) => {
        this.isDeptModalOpen.set(false);
        this.actionMessage.set({ type: 'ok', text: `Department "${d.name}" created.` });
        this.api.getDepartments().subscribe((res) => this.departments.set(res));
      },
      error: (err) => {
        this.actionMessage.set({ type: 'error', text: err.error?.detail || 'Failed to create department.' });
      },
    });
  }

  protected getEligibleParents(): readonly CategoryDto[] {
    if (!this.isEditingCategory() || !this.categoryForm.id) {
      return this.categories();
    }
    // Filter out current category itself
    return this.categories().filter((c) => c.id !== this.categoryForm.id);
  }

  private getEmptyCategoryForm() {
    return {
      code: '',
      name: '',
      description: '',
      parentCategoryId: '',
      departmentId: '',
      hsnCode: '',
      defaultGstRate: 18,
      displayOrder: 1,
    };
  }
}

