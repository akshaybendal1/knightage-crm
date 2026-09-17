import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Auth } from '../../core/auth';
import { CrmApi } from '../../core/crm-api';
import { Product, ProductStage } from '../../core/models';
import { Toast } from '../../core/toast';
import { Loader } from '../../loader/loader';

interface DraftProductLine {
  productId: string;
  productName: string;
  quantity: number;
  productStageId: string;
  productStageName: string;
}

@Component({
  selector: 'app-sales-entry',
  imports: [FormsModule, Loader],
  templateUrl: './sales-entry.html',
  styleUrl: './sales-entry.css',
})
export class SalesEntry implements OnInit {
  loading = signal(false);
  saving = signal(false);
  errorMessage = signal<string | null>(null);
  products = signal<Product[]>([]);
  productStages = signal<ProductStage[]>([]);

  customerName = '';
  company = '';
  email = '';
  phone = '';
  notes = '';

  lines = signal<DraftProductLine[]>([]);
  newProductId = '';
  newQuantity = 1;
  newProductStageId = '';

  // Inline "+ Add new stage" quick-add from the product-stage select, mirroring
  // lead-detail.ts's exact pattern for the same control.
  showNewProductStageInput = false;
  newProductStageName = '';
  savingNewProductStage = signal(false);

  constructor(
    private readonly api: CrmApi,
    protected readonly auth: Auth,
    private readonly toast: Toast,
  ) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.api.getProducts().subscribe({
      next: (products) => {
        this.products.set(products.filter((p) => p.isActive));
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load the product list.');
        this.loading.set(false);
      },
    });
    this.api.getProductStages().subscribe({
      next: (stages) => {
        this.productStages.set(stages);
        // Pre-select the org's default product stage (falling back to Won) --
        // the rep can still change it per line, this just saves a click on the
        // common case.
        const preselected = stages.find((s) => s.isDefault) ?? stages.find((s) => s.isWon);
        if (preselected) {
          this.newProductStageId = preselected.id;
        }
      },
      error: () => {
        // Non-fatal: the stage dropdown just starts empty.
      },
    });
  }

  onProductStageSelectChange(value: string): void {
    if (value === '__new__') {
      this.showNewProductStageInput = true;
      this.newProductStageId = '';
    } else {
      this.showNewProductStageInput = false;
      this.newProductStageId = value;
    }
  }

  createProductStageInline(): void {
    const trimmed = this.newProductStageName.trim();
    if (!trimmed) {
      return;
    }
    this.savingNewProductStage.set(true);
    this.api.createProductStage({ name: trimmed, sortOrder: this.productStages().length + 1, isWon: false, isLost: false }).subscribe({
      next: (stage) => {
        this.productStages.update((s) => [...s, stage]);
        this.newProductStageId = stage.id;
        this.newProductStageName = '';
        this.showNewProductStageInput = false;
        this.savingNewProductStage.set(false);
        this.toast.success('Product stage created.');
      },
      error: () => {
        this.savingNewProductStage.set(false);
        this.toast.error('Could not create product stage.');
      },
    });
  }

  addLine(): void {
    const product = this.products().find((p) => p.id === this.newProductId);
    const stage = this.productStages().find((s) => s.id === this.newProductStageId);
    if (!product || !stage || this.newQuantity < 1) {
      return;
    }
    this.lines.update((lines) => [
      ...lines,
      { productId: product.id, productName: product.name, quantity: this.newQuantity, productStageId: stage.id, productStageName: stage.name },
    ]);
    this.newProductId = '';
    this.newQuantity = 1;
  }

  removeLine(index: number): void {
    this.lines.update((lines) => lines.filter((_, i) => i !== index));
  }

  submit(): void {
    this.errorMessage.set(null);

    if (!this.customerName.trim()) {
      this.errorMessage.set('Enter the customer name.');
      return;
    }
    if (this.lines().length === 0) {
      this.errorMessage.set('Add at least one product.');
      return;
    }
    if (!navigator.geolocation) {
      this.errorMessage.set('This device/browser does not support location -- a sales entry requires it.');
      return;
    }

    this.saving.set(true);
    navigator.geolocation.getCurrentPosition(
      (position) => this.createEntry(position.coords.latitude, position.coords.longitude),
      () => {
        this.saving.set(false);
        this.errorMessage.set('Location access was denied or is unavailable. Allow location access to log a sales entry.');
      },
      { enableHighAccuracy: true, timeout: 15000 },
    );
  }

  private createEntry(latitude: number, longitude: number): void {
    this.api
      .createSalesEntry({
        customerName: this.customerName.trim(),
        company: this.company.trim() || undefined,
        email: this.email.trim() || undefined,
        phone: this.phone.trim() || undefined,
        notes: this.notes.trim() || undefined,
        latitude,
        longitude,
        products: this.lines().map((line) => ({ productId: line.productId, quantity: line.quantity, productStageId: line.productStageId })),
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.toast.success('Sales entry logged.');
          this.resetForm();
        },
        error: (err) => {
          this.saving.set(false);
          this.errorMessage.set(err?.error?.message ?? 'Could not save this sales entry.');
        },
      });
  }

  private resetForm(): void {
    this.customerName = '';
    this.company = '';
    this.email = '';
    this.phone = '';
    this.notes = '';
    this.lines.set([]);
    this.newProductId = '';
    this.newQuantity = 1;
  }
}
