import { Component, OnInit, computed, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Auth } from '../../core/auth';
import { CrmApi } from '../../core/crm-api';
import { Customer, InventorySyncResult, WonLeadListItem } from '../../core/models';
import { Toast } from '../../core/toast';
import { Loader } from '../../loader/loader';

@Component({
  selector: 'app-customers',
  imports: [FormsModule, DatePipe, Loader],
  templateUrl: './customers.html',
})
export class Customers implements OnInit {
  customers = signal<Customer[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);
  pendingInventory = signal(0);
  wonLeadsWithoutCustomer = signal(0);

  search = signal('');
  visible = computed(() => {
    const term = this.search().trim().toLowerCase();
    const active = this.customers().filter((c) => c.isActive);
    if (!term) {
      return active;
    }
    return active.filter((c) => [c.name, c.company, c.email, c.phone].some((v) => (v ?? '').toLowerCase().includes(term)));
  });

  // Importing and sending to Inventory move data in bulk, so the server limits them to Owner/Admin.
  isAdmin = computed(() => ['Owner', 'Admin'].includes(this.auth.currentUser()?.role ?? ''));

  showForm = signal(false);
  name = '';
  company = '';
  email = '';
  phone = '';
  saving = signal(false);

  importing = signal(false);
  syncing = signal(false);

  // "Choose leads" panel: won leads that are not customers yet, paged on the server. The selection is kept
  // across pages and searches (by id) until it is converted or cleared.
  pickerOpen = signal(false);
  pickerLoading = signal(false);
  pickerError = signal<string | null>(null);
  pickerSearch = '';
  pickerPage = signal(1);
  readonly pickerPageSize = 25;
  pickerItems = signal<WonLeadListItem[]>([]);
  pickerTotal = signal(0);
  pickerPages = computed(() => Math.max(1, Math.ceil(this.pickerTotal() / this.pickerPageSize)));
  selected = signal<ReadonlySet<string>>(new Set());
  allOnPageSelected = computed(() => this.pickerItems().length > 0 && this.pickerItems().every((l) => this.selected().has(l.id)));
  converting = signal(false);
  private pickerSearchTimer: ReturnType<typeof setTimeout> | undefined;

  constructor(
    private readonly api: CrmApi,
    protected readonly auth: Auth,
    private readonly toast: Toast,
  ) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getCustomers().subscribe({
      next: (result) => {
        this.customers.set(result.items);
        this.pendingInventory.set(result.pendingInventory);
        this.wonLeadsWithoutCustomer.set(result.wonLeadsWithoutCustomer);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load customers.');
        this.loading.set(false);
      },
    });
  }

  addCustomer(): void {
    const name = this.name.trim();
    if (!name) {
      return;
    }
    this.errorMessage.set(null);
    this.saving.set(true);
    this.api
      .createCustomer({
        name,
        company: this.company.trim() || undefined,
        email: this.email.trim() || undefined,
        phone: this.phone.trim() || undefined,
      })
      .subscribe({
        next: ({ customer, alreadyExisted }) => {
          this.saving.set(false);
          if (alreadyExisted) {
            this.toast.success(`${customer.name} is already a customer — nothing was added.`);
          } else {
            this.toast.success('Customer added.');
          }
          this.name = '';
          this.company = '';
          this.email = '';
          this.phone = '';
          this.showForm.set(false);
          this.load();
        },
        error: (err) => {
          this.saving.set(false);
          const fieldErrors = err?.error?.errors ? (Object.values(err.error.errors) as string[][]).flat() : [];
          this.errorMessage.set(err?.error?.message ?? fieldErrors[0] ?? 'Could not add the customer.');
        },
      });
  }

  importWonLeads(): void {
    this.importing.set(true);
    this.api.importWonLeadsAsCustomers().subscribe({
      next: (result) => {
        this.importing.set(false);
        const parts = [`${result.imported} new customer${result.imported === 1 ? '' : 's'}`];
        if (result.linkedToExisting > 0) {
          parts.push(`${result.linkedToExisting} won lead${result.linkedToExisting === 1 ? '' : 's'} matched an existing customer`);
        }
        let message = `Imported ${parts.join(', ')}.`;
        if (result.remaining > 0) {
          message += ` ${result.remaining} won lead${result.remaining === 1 ? '' : 's'} left — run it again.`;
        }
        this.toast.success(message);
        this.reportInventory(result.inventory);
        this.load();
      },
      error: (err) => {
        this.importing.set(false);
        this.toast.error(err?.error?.message ?? 'Could not import the won leads.');
      },
    });
  }

  togglePicker(): void {
    this.pickerOpen.update((open) => !open);
    if (this.pickerOpen()) {
      this.pickerPage.set(1);
      this.loadPicker();
    }
  }

  onPickerSearch(value: string): void {
    this.pickerSearch = value;
    clearTimeout(this.pickerSearchTimer);
    this.pickerSearchTimer = setTimeout(() => {
      this.pickerPage.set(1);
      this.loadPicker();
    }, 300);
  }

  goToPickerPage(page: number): void {
    if (page < 1 || page > this.pickerPages()) {
      return;
    }
    this.pickerPage.set(page);
    this.loadPicker();
  }

  loadPicker(): void {
    this.pickerLoading.set(true);
    this.pickerError.set(null);
    this.api.getWonLeadsWithoutCustomer(this.pickerSearch, this.pickerPage(), this.pickerPageSize).subscribe({
      next: (result) => {
        // Converting can empty the page being shown (e.g. the last one) -- step back to the last page that has leads.
        const lastPage = Math.max(1, Math.ceil(result.totalCount / this.pickerPageSize));
        if (result.items.length === 0 && this.pickerPage() > lastPage) {
          this.pickerPage.set(lastPage);
          this.loadPicker();
          return;
        }
        this.pickerItems.set(result.items);
        this.pickerTotal.set(result.totalCount);
        this.pickerLoading.set(false);
      },
      error: (err) => {
        this.pickerLoading.set(false);
        this.pickerError.set(err?.error?.message ?? 'Could not load the won leads.');
      },
    });
  }

  isSelected(id: string): boolean {
    return this.selected().has(id);
  }

  toggleLead(id: string): void {
    const next = new Set(this.selected());
    if (next.has(id)) {
      next.delete(id);
    } else {
      next.add(id);
    }
    this.selected.set(next);
  }

  togglePage(): void {
    const next = new Set(this.selected());
    const select = !this.allOnPageSelected();
    for (const lead of this.pickerItems()) {
      if (select) {
        next.add(lead.id);
      } else {
        next.delete(lead.id);
      }
    }
    this.selected.set(next);
  }

  clearSelection(): void {
    this.selected.set(new Set());
  }

  convertSelected(): void {
    const ids = [...this.selected()];
    if (ids.length === 0) {
      return;
    }
    if (ids.length > 500) {
      this.toast.error('Convert at most 500 leads at a time.');
      return;
    }
    this.converting.set(true);
    this.api.convertWonLeadsToCustomers(ids).subscribe({
      next: (result) => {
        this.converting.set(false);
        const parts = [`${result.imported} new customer${result.imported === 1 ? '' : 's'}`];
        if (result.linkedToExisting > 0) {
          parts.push(`${result.linkedToExisting} matched an existing customer`);
        }
        this.toast.success(`Converted: ${parts.join(', ')}.`);
        this.reportInventory(result.inventory);
        this.clearSelection();
        this.load();
        this.loadPicker();
      },
      error: (err) => {
        this.converting.set(false);
        const fieldErrors = err?.error?.errors ? (Object.values(err.error.errors) as string[][]).flat() : [];
        this.toast.error(err?.error?.message ?? fieldErrors[0] ?? 'Could not convert the selected leads.');
      },
    });
  }

  sendToInventory(): void {
    this.syncing.set(true);
    this.api.syncCustomersToInventory().subscribe({
      next: (result) => {
        this.syncing.set(false);
        if (result.available) {
          this.toast.success(result.pushed > 0 ? `Sent ${result.pushed} customer${result.pushed === 1 ? '' : 's'} to Inventory.` : 'Inventory is already up to date.');
        } else {
          this.toast.error(result.message ?? 'Could not reach the Inventory portal.');
        }
        this.load();
      },
      error: (err) => {
        this.syncing.set(false);
        this.toast.error(err?.error?.message ?? 'Could not send the customers to Inventory.');
      },
    });
  }

  // After an import, say so when the customers couldn't be passed on to Inventory (they stay saved here).
  private reportInventory(result: InventorySyncResult): void {
    if (!result.available && result.pending > 0 && result.message) {
      this.toast.error(result.message);
    }
  }

  sourceLabel(customer: Customer): string {
    return customer.source === 'WonLead' ? 'Won lead' : customer.source === 'Lead' ? 'From a lead' : customer.source === 'Inventory' ? 'From Inventory' : 'Added by hand';
  }
}
