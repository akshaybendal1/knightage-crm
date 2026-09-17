import { Component, OnInit, computed, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { CrmApi } from '../../core/crm-api';
import { IdentityApi } from '../../core/identity-api';
import { Lead, MessageTemplate, OrgUser, PipelineStage, Product } from '../../core/models';
import { Toast } from '../../core/toast';
import { Auth } from '../../core/auth';
import { isLeadStuck } from '../../core/stuck-status';
import { WhatsappComposer } from '../../whatsapp-composer/whatsapp-composer';
import { WhatsappBulkPicker } from '../../whatsapp-bulk-picker/whatsapp-bulk-picker';
import { WhatsappQueueDrawer, QueueLead } from '../../whatsapp-queue-drawer/whatsapp-queue-drawer';
import { Loader } from '../../loader/loader';

@Component({
  selector: 'app-leads',
  imports: [FormsModule, RouterLink, CurrencyPipe, WhatsappComposer, WhatsappBulkPicker, WhatsappQueueDrawer, Loader],
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

  protected get yourName(): string {
    return this.auth.currentUser()?.displayName ?? '';
  }

  constructor(
    private readonly api: CrmApi,
    private readonly identityApi: IdentityApi,
    private readonly route: ActivatedRoute,
    private readonly toast: Toast,
    protected readonly auth: Auth,
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

  createLead(): void {
    this.errorMessage.set(null);
    if (!this.pipelineStageId) {
      this.toast.error('Pick a pipeline stage.');
      return;
    }
    this.savingLead.set(true);
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
        next: () => {
          this.savingLead.set(false);
          this.name = '';
          this.email = '';
          this.phone = '';
          this.company = '';
          this.notes = '';
          this.ownerUserId = this.auth.currentUser()?.userId ?? '';
          this.showForm.set(false);
          this.toast.success('Lead created.');
          this.loadLeads();
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
  }

  cancelEdit(): void {
    this.editingId.set(null);
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
