import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CrmApi } from '../../core/crm-api';
import { ProductCategory } from '../../core/models';
import { Toast } from '../../core/toast';
import { Loader } from '../../loader/loader';

@Component({
  selector: 'app-product-categories',
  imports: [FormsModule, Loader],
  templateUrl: './product-categories.html',
})
export class ProductCategories implements OnInit {
  categories = signal<ProductCategory[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);
  showForm = signal(false);

  name = '';
  sortOrder = 1;
  saving = signal(false);

  editingId = signal<string | null>(null);
  editName = '';
  editSortOrder = 1;
  savingEdit = signal(false);

  constructor(
    private readonly api: CrmApi,
    private readonly toast: Toast,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getProductCategories().subscribe({
      next: (categories) => {
        this.categories.set(categories);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load product categories.');
        this.loading.set(false);
      },
    });
  }

  createCategory(): void {
    this.errorMessage.set(null);
    this.saving.set(true);
    this.api.createProductCategory({ name: this.name, sortOrder: this.sortOrder }).subscribe({
      next: () => {
        this.saving.set(false);
        this.name = '';
        this.sortOrder = this.categories().length + 1;
        this.showForm.set(false);
        this.toast.success('Category created.');
        this.load();
      },
      error: () => {
        this.saving.set(false);
        this.toast.error('Could not create category.');
      },
    });
  }

  startEdit(category: ProductCategory): void {
    this.editingId.set(category.id);
    this.editName = category.name;
    this.editSortOrder = category.sortOrder;
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  saveEdit(id: string): void {
    this.savingEdit.set(true);
    this.api.updateProductCategory(id, { name: this.editName, sortOrder: this.editSortOrder }).subscribe({
      next: () => {
        this.savingEdit.set(false);
        this.editingId.set(null);
        this.toast.success('Category updated.');
        this.load();
      },
      error: () => {
        this.savingEdit.set(false);
        this.toast.error('Could not save changes.');
      },
    });
  }

  deleteCategory(category: ProductCategory): void {
    if (!confirm(`Delete the "${category.name}" category? This can't be undone from here.`)) {
      return;
    }
    this.api.deleteProductCategory(category.id).subscribe({
      next: () => {
        this.toast.success('Category deleted.');
        this.load();
      },
      error: () => this.toast.error('Could not delete this category.'),
    });
  }
}
