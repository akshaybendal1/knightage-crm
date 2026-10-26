import { Component, OnInit, computed, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { CrmApi } from '../../core/crm-api';
import { IdentityApi } from '../../core/identity-api';
import { Customer, CustomerMatch, DuplicateResult, Lead, LeadMatch, MatchReason, MessageTemplate, OrgUser, PipelineStage, Product, ProductStage } from '../../core/models';
import { describeBulkMove } from '../../core/stage-move';
import { Toast } from '../../core/toast';
import { StageMoveDialog, StageMoveDialogItem, StageMoveDialogResult } from '../../stage-move-dialog/stage-move-dialog';
import { Auth } from '../../core/auth';
import { ScreenAccess } from '../../core/screen-access';
import { isLeadStuck } from '../../core/stuck-status';
import { WhatsappComposer } from '../../whatsapp-composer/whatsapp-composer';
import { WhatsappBulkPicker } from '../../whatsapp-bulk-picker/whatsapp-bulk-picker';
import { WhatsappQueueDrawer, QueueLead } from '../../whatsapp-queue-drawer/whatsapp-queue-drawer';
import { Loader } from '../../loader/loader';

@Component({
  selector: 'app-leads',
  imports: [FormsModule, RouterLink, CurrencyPipe, WhatsappComposer, WhatsappBulkPicker, WhatsappQueueDrawer, Loader, StageMoveDialog],
  templateUrl: './leads.html',
  styleUrl: './leads.css',
})
export class Leads implements OnInit {
  stages = signal<PipelineStage[]>([]);
  // Products a lead can be filtered by -- the "Filter by product" dropdown still uses
  // this (a lead can have multiple product line items now; multi-product management
  // itself lives on the Lead Detail page, not here).
  products = signal<Product[]>([]);
  activeProducts = computed(() => this.products().filter((p) => p.isActive));
  orgUsers = signal<OrgUser[]>([]);
  leads = signal<Lead[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);
  showForm = signal(false);
  exporting = signal(false);

  readonly pageSize = 20;
  page = signal(1);
  totalCount = signal(0);
  totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));
  pageStart = computed(() => (this.totalCount() === 0 ? 0 : (this.page() - 1) * this.pageSize + 1));
  pageEnd = computed(() => Math.min(this.page() * this.pageSize, this.totalCount()));

  filterStageId = '';
  filterProductId = '';
  filterOwnerId = '';
  filterSearch = '';
  private searchDebounceTimer: ReturnType<typeof setTimeout> | null = null;

  // Manual create form
  name = '';
  email = '';
  phone = '';
  company = '';
  pipelineStageId = '';
  notes = '';
  ownerUserId = '';
  savingLead = signal(false);

  // Inline "+ Add new stage" from the create form's stage select -- for new vendors
  // who haven't set up any pipeline stages yet, so they don't have to leave this
  // page to go create one on the Pipeline Stages admin page first.
  showNewStageInput = false;
  newStageName = '';
  savingNewStage = signal(false);

  // Optional "attach a product" step on the create form -- previously a product could
  // only be added after the lead existed, on the Lead Detail page. Mirrors that same
  // add-product-with-stage flow (including its own inline "+ Add new stage" quick-add)
  // so a lead can be created with its first product line item in one step.
  productStages = signal<ProductStage[]>([]);
  newLeadProductId = '';
  // What the product <select> shows: a real product id, '' (no product) or the '__new__' sentinel that opens the
  // inline "add product" mini form. newLeadProductId only ever holds a real product.
  leadProductSelectValue = '';
  showNewLeadProductInput = false;
  newLeadProductName = '';
  newLeadProductPrice: number | null = null;
  savingNewLeadProduct = signal(false);
  newLeadProductQuantity = 1;
  newLeadProductStageId = '';
  showNewLeadProductStageInput = false;
  newLeadProductStageName = '';
  savingNewLeadProductStage = signal(false);

  // Customer on the New lead form: pick an existing one (the lead is linked to it and takes its details), or tick "also add as a
  // new customer". The picker is a type-ahead (a few matches per keystroke), because an organization can have thousands.
  selectedCustomer = signal<Customer | null>(null);
  addAsCustomer = false;
  customerQuery = '';
  customerResults = signal<Customer[]>([]);
  showCustomerResults = signal(false);
  private customerTimer: ReturnType<typeof setTimeout> | null = null;
  private customerSeq = 0;

  // Duplicate warning for a lead typed in by hand. Matching leads/customers are listed as soon as the details are typed; a
  // same-email or same-phone match must be acknowledged ("add it anyway") before the lead can be saved. Picking an existing
  // customer is already a conscious choice, so it skips the acknowledgement.
  duplicates = signal<DuplicateResult | null>(null);
  confirmDuplicate = false;
  private duplicateTimer: ReturnType<typeof setTimeout> | null = null;
  private duplicateSeq = 0;
  hasStrongDuplicate = computed(() => {
    const d = this.duplicates();
    if (!d || this.selectedCustomer()) {
      return false;
    }
    return d.leads.some((m) => m.matchedOn !== 'name+company') || d.customers.some((m) => m.matchedOn !== 'name+company');
  });

  // Duplicate check while editing a lead in the list. Only what the person actually changed is looked up (an unchanged email or
  // phone that already collides with an older lead is left alone), and only another LEAD with the same email/phone must be
  // acknowledged before saving; a customer with that email is simply shown (a lead for an existing customer is normal).
  editDuplicates = signal<DuplicateResult | null>(null);
  confirmEditDuplicate = false;
  private editOriginal = { name: '', email: '', phone: '', company: '' };
  private editDuplicateTimer: ReturnType<typeof setTimeout> | null = null;
  private editDuplicateSeq = 0;
  editStrongDuplicate = computed(() => {
    const d = this.editDuplicates();
    return !!d && d.leads.some((m) => m.matchedOn !== 'name+company');
  });

  // CSV import form
  importStageId = '';
  importFile: File | null = null;
  importing = signal(false);
  importMessage = signal<string | null>(null);

  // Inline edit state
  editingId = signal<string | null>(null);
  savingEdit = signal(false);
  editName = '';
  editEmail = '';
  editPhone = '';
  editCompany = '';
  editStageId = '';
  editNotes = '';
  editOwnerUserId = '';

  templates = signal<MessageTemplate[]>([]);

  // Mobile card view: which leads have their collapsible detail section open.
  expandedLeadIds = signal<Set<string>>(new Set());

  // Bulk WhatsApp send
  selectedLeadIds = signal<Set<string>>(new Set());
  bulkPickerOpen = signal(false);
  bulkMessageTemplate = signal('');
  queueDrawerOpen = signal(false);
  queueLeads = signal<QueueLead[]>([]);

  // Bulk stage move for the ticked leads (optional note: same for all, or one per lead).
  moveTargetStageId = '';
  moveDialogOpen = signal(false);
  moveBusy = signal(false);
  moveDialogItems = signal<StageMoveDialogItem[]>([]);

  protected get yourName(): string {
    return this.auth.currentUser()?.displayName ?? '';
  }

  constructor(
    private readonly api: CrmApi,
    private readonly identityApi: IdentityApi,
    private readonly route: ActivatedRoute,
    private readonly toast: Toast,
    protected readonly auth: Auth,
    protected readonly screenAccess: ScreenAccess,
  ) {}

  ngOnInit(): void {
    this.filterStageId = this.route.snapshot.queryParamMap.get('pipelineStageId') ?? '';
    this.ownerUserId = this.auth.currentUser()?.userId ?? '';
    this.api.getPipelineStages().subscribe((stages) => this.stages.set(stages));
    this.api.getProducts().subscribe({
      next: (products) => this.products.set(products),
      error: () => {
        // Non-fatal: the product picker just stays empty.
      },
    });
    this.api.getProductStages().subscribe({
      next: (stages) => this.productStages.set(stages),
      error: () => {
        // Non-fatal: the product stage picker just stays empty.
      },
    });
    this.identityApi.getOrganizationUsers().subscribe({
      next: (users) => this.orgUsers.set(users),
      error: () => {
        // Non-fatal: the owner picker just stays empty.
      },
    });
    this.api.getMessageTemplates().subscribe({
      next: (templates) => this.templates.set(templates),
      error: () => {
        // Non-fatal: the composer just falls back to custom-message-only.
      },
    });
    this.loadLeads();
  }

  stageName(id: string): string {
    return this.stages().find((s) => s.id === id)?.name ?? id;
  }

  leadStuck(lead: Lead): boolean {
    return isLeadStuck(lead, this.stages().find((s) => s.id === lead.pipelineStageId));
  }

  // The create form's stage <select> has a synthetic "__new__" option appended after
  // the real stages -- selecting it reveals this inline mini-form instead of setting
  // pipelineStageId to a real stage id. One-way [ngModel] + this single handler (rather
  // than [(ngModel)] plus a second (ngModelChange)) so there's exactly one place that
  // decides what pipelineStageId becomes -- two listeners for the same output on one
  // element don't reliably both fire.
  onStageSelectChange(value: string): void {
    if (value === '__new__') {
      this.showNewStageInput = true;
      this.pipelineStageId = '';
    } else {
      this.showNewStageInput = false;
      this.pipelineStageId = value;
    }
  }

  createStageInline(): void {
    const trimmed = this.newStageName.trim();
    if (!trimmed) {
      return;
    }
    this.savingNewStage.set(true);
    // sortOrder mirrors the Pipeline Stages admin page's own createStage() heuristic --
    // a stage added here shouldn't need the user to also think about ordering.
    this.api.createPipelineStage({ name: trimmed, sortOrder: this.stages().length + 1, isWon: false, isLost: false }).subscribe({
      next: (stage) => {
        this.stages.update((s) => [...s, stage]);
        this.pipelineStageId = stage.id;
        this.newStageName = '';
        this.showNewStageInput = false;
        this.savingNewStage.set(false);
        this.toast.success('Pipeline stage created.');
      },
      error: () => {
        this.savingNewStage.set(false);
        this.toast.error('Could not create pipeline stage.');
      },
    });
  }

  // Same synthetic "__new__" sentinel pattern as onStageSelectChange above, for the
  // optional product-stage select in the "attach a product" part of the create form.
  onLeadProductStageSelectChange(value: string): void {
    if (value === '__new__') {
      this.showNewLeadProductStageInput = true;
      this.newLeadProductStageId = '';
    } else {
      this.showNewLeadProductStageInput = false;
      this.newLeadProductStageId = value;
    }
  }

  createLeadProductStageInline(): void {
    const trimmed = this.newLeadProductStageName.trim();
    if (!trimmed) {
      return;
    }
    this.savingNewLeadProductStage.set(true);
    this.api.createProductStage({ name: trimmed, sortOrder: this.productStages().length + 1, isWon: false, isLost: false }).subscribe({
      next: (stage) => {
        this.productStages.update((s) => [...s, stage]);
        this.newLeadProductStageId = stage.id;
        this.newLeadProductStageName = '';
        this.showNewLeadProductStageInput = false;
        this.savingNewLeadProductStage.set(false);
        this.toast.success('Product stage created.');
      },
      error: () => {
        this.savingNewLeadProductStage.set(false);
        this.toast.error('Could not create product stage.');
      },
    });
  }

  // Adding a product needs the Products screen (Admin only unless the organization changed that on Access
  // Control), the same rule the server applies -- so the option is only offered to people it will work for.
  canAddProducts(): boolean {
    return this.screenAccess.can('products');
  }

  onLeadProductSelectChange(value: string): void {
    this.leadProductSelectValue = value;
    if (value === '__new__') {
      this.showNewLeadProductInput = true;
      this.newLeadProductId = '';
    } else {
      this.showNewLeadProductInput = false;
      this.newLeadProductId = value;
    }
  }

  cancelNewLeadProduct(): void {
    this.newLeadProductName = '';
    this.newLeadProductPrice = null;
    this.onLeadProductSelectChange('');
  }

  createLeadProductInline(): void {
    const name = this.newLeadProductName.trim();
    if (!name) {
      return;
    }
    const price = Number(this.newLeadProductPrice ?? 0);
    if (!Number.isFinite(price) || price < 0) {
      this.toast.error('Enter a price of 0 or more.');
      return;
    }
    this.savingNewLeadProduct.set(true);
    this.api.createProduct({ name, price }).subscribe({
      next: (product) => {
        this.products.update((list) => [...list, product]);
        this.newLeadProductId = product.id;
        this.leadProductSelectValue = product.id;
        this.newLeadProductName = '';
        this.newLeadProductPrice = null;
        this.showNewLeadProductInput = false;
        this.savingNewLeadProduct.set(false);
        this.toast.success('Product added.');
      },
      error: (err) => {
        this.savingNewLeadProduct.set(false);
        this.toast.error(err?.error?.message ?? 'Could not add the product.');
      },
    });
  }

  ownerLabel(userId: string | null | undefined): string {
    if (!userId) {
      return 'Unassigned';
    }
    if (userId === this.auth.currentUser()?.userId) {
      return 'You';
    }
    return this.orgUsers().find((u) => u.id === userId)?.displayName ?? `User ${userId.slice(0, 8)}`;
  }

  onFilterChange(): void {
    this.page.set(1);
    this.loadLeads();
  }

  // Debounced so typing a search term doesn't fire a request per keystroke -- the other
  // filters (stage/product dropdowns) call onFilterChange directly since a select only
  // fires on a completed choice, not per-keystroke.
  onSearchChange(): void {
    if (this.searchDebounceTimer) {
      clearTimeout(this.searchDebounceTimer);
    }
    this.searchDebounceTimer = setTimeout(() => this.onFilterChange(), 300);
  }

  openMoveDialog(): void {
    const stage = this.stages().find((st) => st.id === this.moveTargetStageId);
    const selected = this.selectedLeadIds();
    const leads = this.leads().filter((l) => selected.has(l.id));
    if (!stage || leads.length === 0) {
      return;
    }
    this.moveDialogItems.set(leads.map((l) => ({ id: l.id, label: l.name, sublabel: l.company })));
    this.moveDialogOpen.set(true);
  }

  closeMoveDialog(): void {
    this.moveDialogOpen.set(false);
  }

  confirmBulkMove(result: StageMoveDialogResult): void {
    const stage = this.stages().find((st) => st.id === this.moveTargetStageId);
    if (!stage) {
      return;
    }
    this.moveBusy.set(true);
    this.api.bulkUpdateLeadStage(stage.id, result.items.map((i) => ({ id: i.id, note: i.note || undefined }))).subscribe({
      next: (res) => {
        this.moveBusy.set(false);
        this.moveDialogOpen.set(false);
        this.moveTargetStageId = '';
        const summary = describeBulkMove(res, stage.name);
        if (summary.hasFailures) {
          this.toast.error(summary.text);
        } else {
          this.toast.success(summary.text);
        }
        this.loadLeads(); // also clears the selection
      },
      error: () => {
        this.moveBusy.set(false);
        this.toast.error('Could not move the selected leads. Refresh to see where they are now.');
      },
    });
  }

  loadLeads(): void {
    this.loading.set(true);
    this.selectedLeadIds.set(new Set());
    this.api
      .getLeads(
        this.filterStageId || undefined,
        this.filterProductId || undefined,
        this.filterOwnerId || undefined,
        this.filterSearch.trim() || undefined,
        this.page(),
        this.pageSize,
      )
      .subscribe({
      next: (result) => {
        this.leads.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load leads.');
        this.loading.set(false);
      },
    });
  }

  isSelected(leadId: string): boolean {
    return this.selectedLeadIds().has(leadId);
  }

  toggleSelected(leadId: string): void {
    const next = new Set(this.selectedLeadIds());
    if (next.has(leadId)) {
      next.delete(leadId);
    } else {
      next.add(leadId);
    }
    this.selectedLeadIds.set(next);
  }

  isExpanded(leadId: string): boolean {
    return this.expandedLeadIds().has(leadId);
  }

  toggleExpanded(leadId: string): void {
    const next = new Set(this.expandedLeadIds());
    if (next.has(leadId)) {
      next.delete(leadId);
    } else {
      next.add(leadId);
    }
    this.expandedLeadIds.set(next);
  }

  allSelected(): boolean {
    const leads = this.leads();
    return leads.length > 0 && leads.every((l) => this.selectedLeadIds().has(l.id));
  }

  toggleSelectAll(): void {
    if (this.allSelected()) {
      this.selectedLeadIds.set(new Set());
    } else {
      this.selectedLeadIds.set(new Set(this.leads().map((l) => l.id)));
    }
  }

  openBulkPicker(): void {
    this.bulkPickerOpen.set(true);
  }

  onBulkMessageConfirmed(message: string): void {
    this.bulkMessageTemplate.set(message);
    this.bulkPickerOpen.set(false);
    const selected = this.selectedLeadIds();
    this.queueLeads.set(this.leads().filter((l) => selected.has(l.id)).map((l) => ({ id: l.id, name: l.name, phone: l.phone })));
    this.queueDrawerOpen.set(true);
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }
    this.page.set(page);
    this.loadLeads();
  }

  // --- customer picker (type-ahead) ---------------------------------------------------------------------------------------------

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

  pickCustomer(customer: Customer): void {
    this.selectedCustomer.set(customer);
    this.name = customer.name;
    this.email = customer.email ?? '';
    this.phone = customer.phone ?? '';
    this.company = customer.company ?? '';
    this.addAsCustomer = false;
    this.customerQuery = '';
    this.showCustomerResults.set(false);
    this.duplicates.set(null);
  }

  clearCustomer(): void {
    this.selectedCustomer.set(null);
    this.onIdentityChange();
  }

  // The duplicate panel offers matching customers too: "Use this customer" picks that one.
  useExistingCustomer(match: CustomerMatch): void {
    this.pickCustomer({
      id: match.id,
      name: match.name,
      company: match.company,
      email: match.email,
      phone: match.phone,
      source: '',
      isActive: true,
      createdAtUtc: '',
      leadCount: 0,
    });
  }

  // --- duplicate check ----------------------------------------------------------------------------------------------------------

  // Called whenever name / email / phone / company is typed in. The answer to an earlier warning ("add it anyway") does not carry
  // over to different details, so it is reset; the lookup itself waits for a pause in typing.
  onIdentityChange(): void {
    this.confirmDuplicate = false;
    if (this.duplicateTimer) {
      clearTimeout(this.duplicateTimer);
    }
    this.duplicateTimer = setTimeout(() => this.runDuplicateCheck(), 600);
  }

  private runDuplicateCheck(): void {
    const query = { name: this.name.trim(), company: this.company.trim(), email: this.email.trim(), phone: this.phone.trim() };
    if (!query.name && !query.email && !query.phone) {
      this.duplicates.set(null);
      return;
    }
    const seq = ++this.duplicateSeq;
    this.api.findLeadDuplicates(query).subscribe({
      next: (result) => {
        if (seq === this.duplicateSeq) {
          this.duplicates.set(result.leads.length || result.customers.length ? result : null);
        }
      },
      // A failed check must never get in the way of adding a lead.
      error: () => {
        if (seq === this.duplicateSeq) {
          this.duplicates.set(null);
        }
      },
    });
  }

  // One line of detail under a customer in the type-ahead list.
  customerDetail(customer: Customer): string {
    return [customer.company, customer.email, customer.phone].filter((v) => !!v).join(' · ');
  }

  matchReason(reason: MatchReason): string {
    return reason === 'email' ? 'same email' : reason === 'phone' ? 'same phone number' : 'same name and company';
  }

  matchTitle(match: LeadMatch | CustomerMatch): string {
    return match.company ? `${match.name} (${match.company})` : match.name;
  }

  private resetCustomerAndDuplicates(): void {
    this.selectedCustomer.set(null);
    this.addAsCustomer = false;
    this.customerQuery = '';
    this.customerResults.set([]);
    this.showCustomerResults.set(false);
    this.duplicates.set(null);
    this.confirmDuplicate = false;
  }

  // --- create -------------------------------------------------------------------------------------------------------------------

  createLead(): void {
    this.errorMessage.set(null);
    if (!this.pipelineStageId) {
      this.toast.error('Pick a pipeline stage.');
      return;
    }
    if (this.showNewLeadProductInput) {
      this.toast.error('Add the new product first, or choose "No product".');
      return;
    }
    if (this.newLeadProductId && !this.newLeadProductStageId) {
      this.toast.error('Pick a product stage for the product.');
      return;
    }
    if (this.hasStrongDuplicate() && !this.confirmDuplicate) {
      this.toast.error('This looks like someone who is already in the CRM. Open the existing record, or tick "add it anyway".');
      return;
    }
    this.savingLead.set(true);
    const customer = this.selectedCustomer();
    const makeCustomer = !customer && this.addAsCustomer;
    this.api
      .createLead({
        name: this.name,
        email: this.email || undefined,
        phone: this.phone || undefined,
        company: this.company || undefined,
        pipelineStageId: this.pipelineStageId,
        notes: this.notes || undefined,
        ownerUserId: this.ownerUserId || undefined,
      })
      .subscribe({
        next: (lead) => {
          const productId = this.newLeadProductId;
          const productQuantity = this.newLeadProductQuantity;
          const productStageId = this.newLeadProductStageId;
          const customerInvolved = !!customer || makeCustomer;
          const done: string[] = [];
          const failed: string[] = [];
          const finish = () => {
            this.savingLead.set(false);
            this.name = '';
            this.email = '';
            this.phone = '';
            this.company = '';
            this.notes = '';
            this.ownerUserId = this.auth.currentUser()?.userId ?? '';
            this.newLeadProductId = '';
            this.leadProductSelectValue = '';
            this.showNewLeadProductInput = false;
            this.newLeadProductQuantity = 1;
            this.newLeadProductStageId = '';
            this.resetCustomerAndDuplicates();
            this.showForm.set(false);
            let message: string;
            if (!customerInvolved) {
              // Wording kept exactly as before this form could link customers.
              message = failed.length ? 'Lead created, but could not attach the product.' : productId ? 'Lead created with product attached.' : 'Lead created.';
            } else {
              message = 'Lead created' + (done.length ? ' — ' + done.join(', ') : '') + (failed.length ? `, but could not ${failed.join(' or ')}` : '') + '.';
            }
            failed.length ? this.toast.error(message) : this.toast.success(message);
            this.loadLeads();
          };
          // The lead itself was created successfully -- a failure in either follow-up step is a partial-success message,
          // not the generic create error.
          const attachProduct = () => {
            if (!productId) {
              finish();
              return;
            }
            this.api.addLeadProduct(lead.id, { productId, quantity: productQuantity || 1, productStageId }).subscribe({
              next: () => {
                done.push('product attached');
                finish();
              },
              error: () => {
                failed.push('attach the product');
                finish();
              },
            });
          };
          if (!customerInvolved) {
            attachProduct();
            return;
          }
          this.api.setLeadCustomer(lead.id, customer ? { customerId: customer.id } : { createFromLead: true }).subscribe({
            next: (result) => {
              const who = result.customer?.name ?? customer?.name ?? 'the customer';
              done.push(customer ? `linked to customer ${who}` : result.created ? `added as customer ${who}` : `linked to existing customer ${who}`);
              attachProduct();
            },
            error: () => {
              failed.push('link the customer');
              attachProduct();
            },
          });
        },
        error: () => {
          this.savingLead.set(false);
          this.toast.error('Could not create lead.');
        },
      });
  }

  onImportFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    this.importFile = input.files?.[0] ?? null;
  }

  importLeads(): void {
    if (!this.importFile || !this.importStageId) {
      this.toast.error('Pick a pipeline stage and a CSV file first.');
      return;
    }
    this.importing.set(true);
    this.errorMessage.set(null);
    this.api.importLeads(this.importFile, this.importStageId).subscribe({
      next: (result) => {
        this.importing.set(false);
        this.importFile = null;
        const errorNote = result.errors.length ? ` (${result.errors.length} line(s) skipped)` : '';
        const message = `Imported ${result.importedCount} lead(s)${errorNote}.`;
        this.importMessage.set(message);
        if (result.errors.length) {
          this.toast.error(message);
        } else {
          this.toast.success(message);
        }
        this.loadLeads();
      },
      error: () => {
        this.importing.set(false);
        this.toast.error('Could not import this file.');
      },
    });
  }

  downloadImportTemplate(): void {
    const csv = 'Name,Email,Phone,Company\nJane Doe,jane@example.com,+1 555 0100,Acme Inc.\n';
    const blob = new Blob([csv], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'leads-import-template.csv';
    a.click();
    URL.revokeObjectURL(url);
  }

  exportLeads(): void {
    this.exporting.set(true);
    this.api
      .exportLeads(this.filterStageId || undefined, this.filterProductId || undefined, this.filterOwnerId || undefined, this.filterSearch.trim() || undefined)
      .subscribe({
        next: (blob) => {
          this.exporting.set(false);
          const url = URL.createObjectURL(blob);
          const a = document.createElement('a');
          a.href = url;
          a.download = `leads-${new Date().toISOString().slice(0, 10)}.xlsx`;
          a.click();
          URL.revokeObjectURL(url);
        },
        error: () => {
          this.exporting.set(false);
          this.toast.error('Could not export leads.');
        },
      });
  }

  startEdit(lead: Lead): void {
    this.editingId.set(lead.id);
    this.editName = lead.name;
    this.editEmail = lead.email ?? '';
    this.editPhone = lead.phone ?? '';
    this.editCompany = lead.company ?? '';
    this.editStageId = lead.pipelineStageId;
    this.editNotes = lead.notes ?? '';
    this.editOwnerUserId = lead.ownerUserId ?? '';
    this.editOriginal = { name: lead.name.trim(), email: (lead.email ?? '').trim(), phone: (lead.phone ?? '').trim(), company: (lead.company ?? '').trim() };
    this.resetEditDuplicates();
  }

  cancelEdit(): void {
    this.editingId.set(null);
    this.resetEditDuplicates();
  }

  private resetEditDuplicates(): void {
    if (this.editDuplicateTimer) {
      clearTimeout(this.editDuplicateTimer);
    }
    this.editDuplicateSeq++;
    this.editDuplicates.set(null);
    this.confirmEditDuplicate = false;
  }

  // Called whenever name / email / phone / company is changed in the edit row. A new answer to the warning is needed for new details.
  onEditIdentityChange(): void {
    this.confirmEditDuplicate = false;
    if (this.editDuplicateTimer) {
      clearTimeout(this.editDuplicateTimer);
    }
    this.editDuplicateTimer = setTimeout(() => this.runEditDuplicateCheck(), 600);
  }

  private runEditDuplicateCheck(): void {
    const id = this.editingId();
    if (!id) {
      return;
    }
    const o = this.editOriginal;
    const name = this.editName.trim();
    const company = this.editCompany.trim();
    const email = this.editEmail.trim();
    const phone = this.editPhone.trim();
    const emailChanged = email.toLowerCase() !== o.email.toLowerCase();
    const phoneChanged = phone !== o.phone;
    const nameChanged = name.toLowerCase() !== o.name.toLowerCase() || company.toLowerCase() !== o.company.toLowerCase();
    const query = {
      excludeId: id,
      email: emailChanged ? email : undefined,
      phone: phoneChanged ? phone : undefined,
      name: nameChanged ? name : undefined,
      company: nameChanged ? company : undefined,
    };
    if (!query.email && !query.phone && !query.name) {
      this.editDuplicates.set(null);
      return;
    }
    const seq = ++this.editDuplicateSeq;
    this.api.findLeadDuplicates(query).subscribe({
      next: (result) => {
        if (seq === this.editDuplicateSeq && this.editingId() === id) {
          this.editDuplicates.set(result.leads.length || result.customers.length ? result : null);
        }
      },
      // A failed check must never get in the way of saving.
      error: () => {
        if (seq === this.editDuplicateSeq) {
          this.editDuplicates.set(null);
        }
      },
    });
  }

  saveEdit(id: string): void {
    this.savingEdit.set(true);
    this.api
      .updateLead(id, {
        name: this.editName,
        email: this.editEmail || undefined,
        phone: this.editPhone || undefined,
        company: this.editCompany || undefined,
        pipelineStageId: this.editStageId,
        notes: this.editNotes || undefined,
        ownerUserId: this.editOwnerUserId || undefined,
      })
      .subscribe({
        next: () => {
          this.savingEdit.set(false);
          this.editingId.set(null);
          this.resetEditDuplicates();
          this.toast.success('Lead updated.');
          this.loadLeads();
        },
        error: () => {
          this.savingEdit.set(false);
          this.toast.error('Could not save changes.');
        },
      });
  }

  deleteLead(lead: Lead): void {
    if (!confirm(`Delete ${lead.name}? This can't be undone from here.`)) {
      return;
    }
    this.api.deleteLead(lead.id).subscribe({
      next: () => {
        this.toast.success('Lead deleted.');
        this.loadLeads();
      },
      error: () => this.toast.error('Could not delete this lead.'),
    });
  }
}
