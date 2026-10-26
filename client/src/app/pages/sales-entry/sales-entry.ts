import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Auth } from '../../core/auth';
import { CrmApi } from '../../core/crm-api';
import { Customer, Product, ProductStage } from '../../core/models';
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

  // The customer is picked from the organization's customers (people whose lead was won, or who were added before)
  // instead of typed in -- so the same customer is never entered twice under slightly different spellings. It is a
  // type-ahead (a few matches per keystroke) rather than a drop-down of everyone, because an organization can have
  // thousands of customers. "+ Add new customer" sits at the end of the results and opens the small form below.
  selectedCustomer = signal<Customer | null>(null);
  customerQuery = '';
  customerResults = signal<Customer[]>([]);
  showCustomerResults = signal(false);
  private customerTimer: ReturnType<typeof setTimeout> | null = null;
  private customerSeq = 0;

  showNewCustomerForm = false;
  newCustomerName = '';
  newCustomerCompany = '';
  newCustomerEmail = '';
  newCustomerPhone = '';
  savingNewCustomer = signal(false);
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
    // Customers added in the Inventory portal join the CRM's list at most every couple of minutes; asking once as the form
    // opens means they are already searchable by the time the rep gets to the customer box. Never blocks or fails the form.
    this.api.refreshCustomersFromInventory().subscribe({
      error: () => {
        // Non-fatal: the search simply shows what the CRM already has.
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

  // --- customer picker (type-ahead) ----------------------------------------------------------------------------------------------

  onCustomerFocus(): void {
    this.showCustomerResults.set(true);
    this.searchCustomers();
  }

  onCustomerQueryChange(): void {
    this.showCustomerResults.set(true);
    if (this.customerTimer) {
      clearTimeout(this.customerTimer);
    }
    this.customerTimer = setTimeout(() => this.searchCustomers(), 250);
  }

  // Closed a moment late so a click on a result lands before the list disappears.
  onCustomerBlur(): void {
    setTimeout(() => this.showCustomerResults.set(false), 180);
  }

  private searchCustomers(): void {
    const seq = ++this.customerSeq;
    this.api.searchCustomers(this.customerQuery.trim()).subscribe({
      next: (rows) => {
        if (seq === this.customerSeq) {
          this.customerResults.set(rows);
        }
      },
      error: () => {
        if (seq === this.customerSeq) {
          this.customerResults.set([]);
        }
      },
    });
  }

  // One line of detail under a customer in the results list.
  customerDetail(customer: Customer): string {
    return [customer.company, customer.email, customer.phone].filter((v) => !!v).join(' · ');
  }

  pickCustomer(customer: Customer): void {
    this.selectedCustomer.set(customer);
    this.customerQuery = '';
    this.showCustomerResults.set(false);
    this.showNewCustomerForm = false;
    // A message about the previous choice ("Choose a customer", "Add the new customer first") no longer applies.
    this.errorMessage.set(null);
  }

  clearCustomer(): void {
    this.selectedCustomer.set(null);
  }

  // Opens the small "new customer" form, starting from whatever was typed in the search box.
  startNewCustomer(): void {
    this.newCustomerName = this.customerQuery.trim();
    this.showNewCustomerForm = true;
    this.showCustomerResults.set(false);
    this.errorMessage.set(null);
  }

  cancelNewCustomer(): void {
    this.showNewCustomerForm = false;
    this.errorMessage.set(null);
  }

  createCustomerInline(): void {
    const name = this.newCustomerName.trim();
    if (!name) {
      return;
    }
    this.savingNewCustomer.set(true);
    this.api
      .createCustomer({
        name,
        company: this.newCustomerCompany.trim() || undefined,
        email: this.newCustomerEmail.trim() || undefined,
        phone: this.newCustomerPhone.trim() || undefined,
      })
      .subscribe({
        next: ({ customer, alreadyExisted }) => {
          this.selectedCustomer.set(customer);
          this.errorMessage.set(null);
          this.customerQuery = '';
          this.newCustomerName = '';
          this.newCustomerCompany = '';
          this.newCustomerEmail = '';
          this.newCustomerPhone = '';
          this.showNewCustomerForm = false;
          this.savingNewCustomer.set(false);
          this.toast.success(alreadyExisted ? `${customer.name} is already a customer — selected.` : 'Customer added.');
        },
        error: (err) => {
          this.savingNewCustomer.set(false);
          this.toast.error(this.firstError(err) ?? 'Could not add the customer.');
        },
      });
  }

  // ASP.NET answers a failed model check with { errors: { Field: [messages] } } and our own checks with { message }.
  private firstError(err: { error?: { message?: string; errors?: Record<string, string[]> } }): string | null {
    const fieldErrors = err?.error?.errors ? Object.values(err.error.errors).flat() : [];
    return err?.error?.message ?? fieldErrors[0] ?? null;
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

    const customer = this.selectedCustomer();
    if (!customer) {
      this.errorMessage.set(this.showNewCustomerForm ? 'Add the new customer first, or pick an existing one.' : 'Choose a customer, or add a new one.');
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
      (position) => this.createEntry(customer, position.coords.latitude, position.coords.longitude),
      () => {
        this.saving.set(false);
        this.errorMessage.set('Location access was denied or is unavailable. Allow location access to log a sales entry.');
      },
      { enableHighAccuracy: true, timeout: 15000 },
    );
  }

  private createEntry(customer: Customer, latitude: number, longitude: number): void {
    this.api
      .createSalesEntry({
        customerId: customer.id,
        customerName: customer.name,
        company: customer.company || undefined,
        email: customer.email || undefined,
        phone: customer.phone || undefined,
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
    this.selectedCustomer.set(null);
    this.customerQuery = '';
    this.customerResults.set([]);
    this.showCustomerResults.set(false);
    this.showNewCustomerForm = false;
    this.notes = '';
    this.lines.set([]);
    this.newProductId = '';
    this.newQuantity = 1;
  }
}
