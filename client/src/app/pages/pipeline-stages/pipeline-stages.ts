import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CrmApi } from '../../core/crm-api';
import { PipelineStage, ProductStage } from '../../core/models';
import { Toast } from '../../core/toast';
import { Loader } from '../../loader/loader';

@Component({
  selector: 'app-pipeline-stages',
  imports: [FormsModule, Loader],
  templateUrl: './pipeline-stages.html',
  styleUrl: './pipeline-stages.css',
})
export class PipelineStages implements OnInit {
  activeTab = signal<'pipeline' | 'product'>('pipeline');

  stages = signal<PipelineStage[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);
  showForm = signal(false);

  name = '';
  sortOrder = 1;
  isWon = false;
  isLost = false;
  isDefault = false;
  savingStage = signal(false);

  editingId = signal<string | null>(null);
  editName = '';
  editSortOrder = 1;
  editIsWon = false;
  editIsLost = false;
  editIsDefault = false;
  savingEdit = signal(false);

  // Product stages -- a parallel resource on this same page, mirroring the
  // Pipeline Stages section above field-for-field and method-for-method.
  productStages = signal<ProductStage[]>([]);
  productLoading = signal(false);
  productErrorMessage = signal<string | null>(null);
  showProductForm = signal(false);

  productName = '';
  productSortOrder = 1;
  productIsWon = false;
  productIsLost = false;
  productIsDefault = false;
  savingProductStage = signal(false);

  editingProductId = signal<string | null>(null);
  editProductName = '';
  editProductSortOrder = 1;
  editProductIsWon = false;
  editProductIsLost = false;
  editProductIsDefault = false;
  savingProductEdit = signal(false);

  constructor(
    private readonly api: CrmApi,
    private readonly toast: Toast,
  ) {}

  ngOnInit(): void {
    this.load();
    this.loadProductStages();
  }

  load(): void {
    this.loading.set(true);
    this.api.getPipelineStages().subscribe({
      next: (stages) => {
        this.stages.set(stages);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load pipeline stages.');
        this.loading.set(false);
      },
    });
  }

  createStage(): void {
    this.errorMessage.set(null);
    this.savingStage.set(true);
    this.api.createPipelineStage({ name: this.name, sortOrder: this.sortOrder, isWon: this.isWon, isLost: this.isLost, isDefault: this.isDefault }).subscribe({
      next: () => {
        this.savingStage.set(false);
        this.name = '';
        this.sortOrder = this.stages().length + 1;
        this.isWon = false;
        this.isLost = false;
        this.isDefault = false;
        this.showForm.set(false);
        this.toast.success('Pipeline stage created.');
        this.load();
      },
      error: () => {
        this.savingStage.set(false);
        this.toast.error('Could not create pipeline stage.');
      },
    });
  }

  startEdit(stage: PipelineStage): void {
    this.editingId.set(stage.id);
    this.editName = stage.name;
    this.editSortOrder = stage.sortOrder;
    this.editIsWon = stage.isWon;
    this.editIsLost = stage.isLost;
    this.editIsDefault = stage.isDefault;
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  saveEdit(id: string): void {
    this.savingEdit.set(true);
    this.api
      .updatePipelineStage(id, {
        name: this.editName,
        sortOrder: this.editSortOrder,
        isWon: this.editIsWon,
        isLost: this.editIsLost,
        isDefault: this.editIsDefault,
      })
      .subscribe({
        next: () => {
          this.savingEdit.set(false);
          this.editingId.set(null);
          this.toast.success('Pipeline stage updated.');
          this.load();
        },
        error: () => {
          this.savingEdit.set(false);
          this.toast.error('Could not save changes.');
        },
      });
  }

  deleteStage(stage: PipelineStage): void {
    if (!confirm(`Delete the "${stage.name}" stage? This can't be undone from here.`)) {
      return;
    }
    this.api.deletePipelineStage(stage.id).subscribe({
      next: () => {
        this.toast.success('Pipeline stage deleted.');
        this.load();
      },
      error: () => this.toast.error('Could not delete this stage.'),
    });
  }

  // ---- Product stages ----

  loadProductStages(): void {
    this.productLoading.set(true);
    this.api.getProductStages().subscribe({
      next: (stages) => {
        this.productStages.set(stages);
        this.productLoading.set(false);
      },
      error: () => {
        this.productErrorMessage.set('Could not load product stages.');
        this.productLoading.set(false);
      },
    });
  }

  createProductStage(): void {
    this.productErrorMessage.set(null);
    this.savingProductStage.set(true);
    this.api
      .createProductStage({ name: this.productName, sortOrder: this.productSortOrder, isWon: this.productIsWon, isLost: this.productIsLost, isDefault: this.productIsDefault })
      .subscribe({
        next: () => {
          this.savingProductStage.set(false);
          this.productName = '';
          this.productSortOrder = this.productStages().length + 1;
          this.productIsWon = false;
          this.productIsLost = false;
          this.productIsDefault = false;
          this.showProductForm.set(false);
          this.toast.success('Product stage created.');
          this.loadProductStages();
        },
        error: () => {
          this.savingProductStage.set(false);
          this.toast.error('Could not create product stage.');
        },
      });
  }

  startEditProduct(stage: ProductStage): void {
    this.editingProductId.set(stage.id);
    this.editProductName = stage.name;
    this.editProductSortOrder = stage.sortOrder;
    this.editProductIsWon = stage.isWon;
    this.editProductIsLost = stage.isLost;
    this.editProductIsDefault = stage.isDefault;
  }

  cancelEditProduct(): void {
    this.editingProductId.set(null);
  }

  saveEditProduct(id: string): void {
    this.savingProductEdit.set(true);
    this.api
      .updateProductStage(id, {
        name: this.editProductName,
        sortOrder: this.editProductSortOrder,
        isWon: this.editProductIsWon,
        isLost: this.editProductIsLost,
        isDefault: this.editProductIsDefault,
      })
      .subscribe({
        next: () => {
          this.savingProductEdit.set(false);
          this.editingProductId.set(null);
          this.toast.success('Product stage updated.');
          this.loadProductStages();
        },
        error: () => {
          this.savingProductEdit.set(false);
          this.toast.error('Could not save changes.');
        },
      });
  }

  deleteProductStage(stage: ProductStage): void {
    if (!confirm(`Delete the "${stage.name}" product stage? This can't be undone from here.`)) {
      return;
    }
    this.api.deleteProductStage(stage.id).subscribe({
      next: () => {
        this.toast.success('Product stage deleted.');
        this.loadProductStages();
      },
      error: () => this.toast.error('Could not delete this stage.'),
    });
  }
}
