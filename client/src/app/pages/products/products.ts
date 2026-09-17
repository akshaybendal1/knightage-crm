import { Component, OnInit, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CrmApi } from '../../core/crm-api';
import { Product, ProductCategory } from '../../core/models';
import { Toast } from '../../core/toast';
import { Loader } from '../../loader/loader';

@Component({
  selector: 'app-products',
  imports: [FormsModule, CurrencyPipe, Loader],
  templateUrl: './products.html',
})
export class Products implements OnInit {
  products = signal<Product[]>([]);
  categories = signal<ProductCategory[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);
  showForm = signal(false);

  name = '';
  description = '';
  category = '';
  price: number | null = null;
  savingProduct = signal(false);

  editingId = signal<string | null>(null);
  editName = '';
  editDescription = '';
  editCategory = '';
  editPrice: number | null = null;
  savingEdit = signal(false);

  constructor(
    private readonly api: CrmApi,
    private readonly toast: Toast,
  ) {}

  ngOnInit(): void {
    this.load();
    this.loadCategories();
  }

  loadCategories(): void {
    this.api.getProductCategories().subscribe({
      next: (categories) => this.categories.set(categories),
      error: () => this.toast.error('Could not load product categories.'),
    });
  }

  load(): void {
    this.loading.set(true);
    this.api.getProducts().subscribe({
      next: (products) => {
        this.products.set(products);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load products.');
        this.loading.set(false);
      },
    });
  }

  createProduct(): void {
    if (this.price === null) {
      return;
    }
    this.errorMessage.set(null);
    this.savingProduct.set(true);
    this.api.createProduct({ name: this.name, description: this.description, category: this.category || null, price: this.price }).subscribe({
      next: () => {
        this.savingProduct.set(false);
        this.name = '';
        this.description = '';
        this.category = '';
        this.price = null;
        this.showForm.set(false);
        this.toast.success('Product created.');
        this.load();
      },
      error: () => {
        this.savingProduct.set(false);
        this.toast.error('Could not create this product.');
      },
    });
  }

  startEdit(product: Product): void {
    this.editingId.set(product.id);
    this.editName = product.name;
    this.editDescription = product.description ?? '';
    this.editCategory = product.category ?? '';
    this.editPrice = product.price;
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  saveEdit(id: string): void {
    if (this.editPrice === null) {
      return;
    }
    this.savingEdit.set(true);
    this.api.updateProduct(id, { name: this.editName, description: this.editDescription, category: this.editCategory || null, price: this.editPrice }).subscribe({
      next: () => {
        this.savingEdit.set(false);
        this.editingId.set(null);
        this.toast.success('Product updated.');
        this.load();
      },
      error: () => {
        this.savingEdit.set(false);
        this.toast.error('Could not save changes.');
      },
    });
  }

  toggleActive(product: Product): void {
    const activating = !product.isActive;
    this.api.setProductActive(product.id, activating).subscribe({
      next: () => {
        this.toast.success(activating ? 'Product reactivated.' : 'Product deactivated.');
        this.load();
      },
      error: () => this.toast.error('Could not update this product.'),
    });
  }
}
