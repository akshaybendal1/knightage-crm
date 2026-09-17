import { Component, OnInit, computed, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { CrmApi } from '../../../core/crm-api';
import { IdentityApi } from '../../../core/identity-api';
import { Auth } from '../../../core/auth';
import { AppConfig } from '../../../core/app-config';
import { requestCalendarConnection } from '../../../core/google-calendar';
import { DocumentType, Lead, LeadActivity, LeadDocument, LeadProduct, LeadTask, Meeting, MessageTemplate, OrgUser, PipelineStage, Product, ProductStage } from '../../../core/models';
import { timeAgo } from '../../../core/time-ago';
import { isOverdue } from '../../../core/task-status';
import { isLeadStuck, isLeadProductStuck } from '../../../core/stuck-status';
import { Toast } from '../../../core/toast';
import { WhatsappComposer } from '../../../whatsapp-composer/whatsapp-composer';
import { Loader } from '../../../loader/loader';

const ACTIVITIES_PAGE_SIZE = 20;

@Component({
  selector: 'app-lead-detail',
  imports: [FormsModule, RouterLink, DatePipe, CurrencyPipe, WhatsappComposer, Loader],
  templateUrl: './lead-detail.html',
  styleUrl: './lead-detail.css',
})
export class LeadDetail implements OnInit {
  lead = signal<Lead | null>(null);
  stages = signal<PipelineStage[]>([]);
  products = signal<Product[]>([]);
  activeProducts = computed(() => this.products().filter((p) => p.isActive));
  activities = signal<LeadActivity[]>([]);
  hasMoreActivities = signal(false);
  loadingMoreActivities = signal(false);
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  showNoteForm = signal(false);
  newNote = '';
  savingNote = signal(false);

  tasks = signal<LeadTask[]>([]);
  orgUsers = signal<OrgUser[]>([]);
  showTaskForm = signal(false);
  savingTask = signal(false);
  newTaskTitle = '';
  newTaskDescription = '';
  newTaskDueDate = '';
  newTaskAssigneeId = '';

  readonly timeAgo = timeAgo;
  readonly isOverdue = isOverdue;

  templates = signal<MessageTemplate[]>([]);

  leadProducts = signal<LeadProduct[]>([]);
  productStages = signal<ProductStage[]>([]);
  showLeadProductForm = signal(false);
  savingLeadProduct = signal(false);
  newLeadProductId = '';
  newLeadProductQuantity = 1;
  newLeadProductStageId = '';

  // Inline "+ Add new stage" quick-add from the Add-product form's stage
  // select -- mirrors the exact "__new__" sentinel pattern already used by
  // the New Lead form's pipeline-stage select (pages/leads/leads.ts).
  showNewProductStageInput = false;
  newProductStageName = '';
  savingNewProductStage = signal(false);

  documents = signal<LeadDocument[]>([]);
  showDocumentForm = signal(false);
  savingDocument = signal(false);
  sendingDocumentId = signal<string | null>(null);
  newDocumentType: DocumentType = 'Quotation';
  newDocumentTaxRate = 0;
  newDocumentDiscount = 0;

  // Kill switch: Google Calendar sync isn't usable in production yet (no Client Secret
  // configured in Google Cloud Console). Meeting scheduling itself works fine without it,
  // so only the connect banner/sync badges are hidden -- flip this back to true once the
  // Client Secret is set, no other code changes needed.
  protected readonly googleCalendarEnabled = false;

  meetings = signal<Meeting[]>([]);
  // null until the connection check comes back -- avoids flashing the "Connect Google
  // Calendar" banner for a moment before we actually know either way.
  calendarConnected = signal<boolean | null>(null);
  showMeetingForm = signal(false);
  savingMeeting = signal(false);
  connectingCalendar = signal(false);
  newMeetingTitle = '';
  newMeetingDescription = '';
  newMeetingStartLocal = '';
  newMeetingEndLocal = '';
  newMeetingLocation = '';
  newMeetingOwnerId = '';

  protected get yourName(): string {
    return this.auth.currentUser()?.displayName ?? '';
  }

  private leadId = '';
  private activitiesPage = 1;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly api: CrmApi,
    private readonly identityApi: IdentityApi,
    protected readonly auth: Auth,
    private readonly toast: Toast,
    protected readonly appConfig: AppConfig,
  ) {}

  ngOnInit(): void {
    this.leadId = this.route.snapshot.paramMap.get('id') ?? '';
    this.newTaskAssigneeId = this.auth.currentUser()?.userId ?? '';
    this.newMeetingOwnerId = this.auth.currentUser()?.userId ?? '';
    this.api.getPipelineStages().subscribe((stages) => this.stages.set(stages));
    this.api.getProductStages().subscribe({
      next: (stages) => this.productStages.set(stages),
      error: () => {
        // Non-fatal: the Add-product form's stage dropdown just starts empty.
      },
    });
    this.api.getProducts().subscribe({
      next: (products) => this.products.set(products),
      error: () => {
        // Non-fatal: the product name just falls back to the raw id.
      },
    });
    this.identityApi.getOrganizationUsers().subscribe({
      next: (users) => this.orgUsers.set(users),
      error: () => {
        // Non-fatal: the assignee dropdown just falls back to "me" only.
      },
    });
    this.api.getMessageTemplates().subscribe({
      next: (templates) => this.templates.set(templates),
      error: () => {
        // Non-fatal: the composer just falls back to custom-message-only.
      },
    });
    this.loadLead();
    this.loadActivities();
    this.loadTasks();
    this.loadLeadProducts();
    this.loadDocuments();
    this.loadMeetings();
    this.openRequestedSection();
    if (this.googleCalendarEnabled) {
      this.loadCalendarConnection();
    }
  }

  stageName(id: string): string {
    return this.stages().find((s) => s.id === id)?.name ?? id;
  }

  leadStuck(l: Lead): boolean {
    return isLeadStuck(l, this.stages().find((s) => s.id === l.pipelineStageId));
  }

  productStuck(item: LeadProduct): boolean {
    return isLeadProductStuck(item, this.productStages().find((s) => s.id === item.productStageId));
  }

  productStatusClass(item: LeadProduct): string {
    const stage = this.productStages().find((s) => s.id === item.productStageId);
    if (stage?.isWon) return 'badge-success';
    if (stage?.isLost) return 'badge-danger';
    return 'badge-primary';
  }

  // Set by openRequestedSection(), consumed once loadLead()'s response comes back and the
  // requested section actually exists in the DOM (see loadLead() for why the scroll can't
  // happen here directly).
  private pendingScrollElementId: string | null = null;

  // Supports the Leads list's mobile quick-add buttons (?open=meeting, etc.) --
  // arriving here with that query param opens the matching form and scrolls to it,
  // instead of making the user hunt for the right section themselves.
  private openRequestedSection(): void {
    const open = this.route.snapshot.queryParamMap.get('open');
    const sections: Record<string, { show: () => void; elementId: string }> = {
      meeting: { show: () => this.showMeetingForm.set(true), elementId: 'meetings-heading' },
      note: { show: () => this.showNoteForm.set(true), elementId: 'notes-heading' },
      task: { show: () => this.showTaskForm.set(true), elementId: 'tasks-heading' },
      product: { show: () => this.showLeadProductForm.set(true), elementId: 'products-heading' },
      document: { show: () => this.showDocumentForm.set(true), elementId: 'documents-heading' },
    };
    const target = open ? sections[open] : undefined;
    if (!target) {
      return;
    }
    target.show();
    this.pendingScrollElementId = target.elementId;
  }

  // Deliberately separate from userLabel() -- that method's null-fallback is "System"
  // (correct for audit fields like createdByUserId), but an unassigned owner should
  // read "Unassigned", not "System".
  ownerLabel(userId: string | null | undefined): string {
    if (!userId) {
      return 'Unassigned';
    }
    if (userId === this.auth.currentUser()?.userId) {
      return 'You';
    }
    return this.orgUsers().find((u) => u.id === userId)?.displayName ?? `User ${userId.slice(0, 8)}`;
  }

  authorLabel(activity: LeadActivity): string {
    if (!activity.createdByUserId) {
      return 'System';
    }
    if (activity.createdByUserId === this.auth.currentUser()?.userId) {
      return 'You';
    }
    const user = this.orgUsers().find((u) => u.id === activity.createdByUserId);
    return user?.displayName ?? `User ${activity.createdByUserId.slice(0, 8)}`;
  }

  activityTypeClass(type: string): string {
    switch (type) {
      case 'StageChange':
        return 'badge-stage-change';
      case 'LeadCreated':
        return 'badge-lead-created';
      case 'TaskCompleted':
        return 'badge-task-completed';
      case 'ProductStatusChange':
        return 'badge-stage-change';
      case 'DocumentCreated':
      case 'DocumentSent':
      case 'DocumentPaid':
        return 'badge-stage-change';
      default:
        return 'badge-note';
    }
  }

  activityTypeLabel(type: string): string {
    switch (type) {
      case 'StageChange':
        return 'Stage change';
      case 'LeadCreated':
        return 'Created';
      case 'TaskCompleted':
        return 'Task completed';
      case 'ProductStatusChange':
        return 'Product status';
      case 'DocumentCreated':
        return 'Document created';
      case 'DocumentSent':
        return 'Document sent';
      case 'DocumentPaid':
        return 'Document paid';
      default:
        return 'Note';
    }
  }

  loadLead(): void {
    this.loading.set(true);
    this.api.getLead(this.leadId).subscribe({
      next: (lead) => {
        this.lead.set(lead);
        this.loading.set(false);
        // The requested section only exists in the DOM once loading() flips to false and
        // this @else if block renders -- scrolling any earlier (e.g. right after ngOnInit,
        // before this response comes back) would target an element that isn't there yet.
        if (this.pendingScrollElementId) {
          const elementId = this.pendingScrollElementId;
          this.pendingScrollElementId = null;
          setTimeout(() => document.getElementById(elementId)?.scrollIntoView({ behavior: 'smooth', block: 'start' }), 0);
        }
      },
      error: () => {
        this.errorMessage.set('Could not load this lead.');
        this.loading.set(false);
      },
    });
  }

  loadActivities(): void {
    this.activitiesPage = 1;
    this.api.getLeadActivities(this.leadId, this.activitiesPage, ACTIVITIES_PAGE_SIZE).subscribe({
      next: (result) => {
        this.activities.set(result.items);
        this.hasMoreActivities.set(result.hasMore);
      },
      error: () => this.errorMessage.set('Could not load the activity timeline.'),
    });
  }

  loadMoreActivities(): void {
    this.loadingMoreActivities.set(true);
    const nextPage = this.activitiesPage + 1;
    this.api.getLeadActivities(this.leadId, nextPage, ACTIVITIES_PAGE_SIZE).subscribe({
      next: (result) => {
        this.activitiesPage = nextPage;
        this.activities.update((existing) => [...existing, ...result.items]);
        this.hasMoreActivities.set(result.hasMore);
        this.loadingMoreActivities.set(false);
      },
      error: () => {
        this.loadingMoreActivities.set(false);
        this.errorMessage.set('Could not load more activity.');
      },
    });
  }

  addNote(): void {
    if (!this.newNote.trim()) {
      return;
    }
    this.savingNote.set(true);
    this.api.createLeadActivity(this.leadId, { content: this.newNote.trim(), type: 'Note' }).subscribe({
      next: () => {
        this.newNote = '';
        this.savingNote.set(false);
        this.showNoteForm.set(false);
        this.toast.success('Note added.');
        this.loadActivities();
      },
      error: () => {
        this.savingNote.set(false);
        this.toast.error('Could not save this note.');
      },
    });
  }

  assigneeLabel(userId: string): string {
    if (userId === this.auth.currentUser()?.userId) {
      return 'You';
    }
    return this.orgUsers().find((u) => u.id === userId)?.displayName ?? `User ${userId.slice(0, 8)}`;
  }

  userLabel(userId: string | null | undefined): string {
    if (!userId) {
      return 'System';
    }
    if (userId === this.auth.currentUser()?.userId) {
      return 'You';
    }
    return this.orgUsers().find((u) => u.id === userId)?.displayName ?? `User ${userId.slice(0, 8)}`;
  }

  loadTasks(): void {
    this.api.getLeadTasks(this.leadId).subscribe({
      next: (tasks) => this.tasks.set(tasks),
      error: () => this.errorMessage.set('Could not load tasks.'),
    });
  }

  addTask(): void {
    if (!this.newTaskTitle.trim() || !this.newTaskDueDate) {
      return;
    }
    this.savingTask.set(true);
    this.api
      .createTask(this.leadId, {
        title: this.newTaskTitle.trim(),
        description: this.newTaskDescription.trim() || undefined,
        dueDate: this.newTaskDueDate,
        assignedToUserId: this.newTaskAssigneeId || undefined,
      })
      .subscribe({
        next: () => {
          this.newTaskTitle = '';
          this.newTaskDescription = '';
          this.newTaskDueDate = '';
          this.newTaskAssigneeId = this.auth.currentUser()?.userId ?? '';
          this.savingTask.set(false);
          this.showTaskForm.set(false);
          this.toast.success('Task added.');
          this.loadTasks();
        },
        error: () => {
          this.savingTask.set(false);
          this.toast.error('Could not create this task.');
        },
      });
  }

  toggleTaskComplete(task: LeadTask): void {
    const nextStatus = task.status === 'Completed' ? 'Open' : 'Completed';
    this.api.updateTaskStatus(task.id, nextStatus).subscribe({
      next: () => {
        this.toast.success(nextStatus === 'Completed' ? 'Task completed.' : 'Task reopened.');
        this.loadTasks();
        this.loadActivities();
      },
      error: () => this.toast.error('Could not update this task.'),
    });
  }

  loadLeadProducts(): void {
    this.api.getLeadProducts(this.leadId).subscribe({
      next: (items) => this.leadProducts.set(items),
      error: () => this.errorMessage.set('Could not load products.'),
    });
  }

  addLeadProduct(): void {
    if (!this.newLeadProductId || this.newLeadProductQuantity < 1 || !this.newLeadProductStageId) {
      return;
    }
    this.savingLeadProduct.set(true);
    this.api
      .addLeadProduct(this.leadId, { productId: this.newLeadProductId, quantity: this.newLeadProductQuantity, productStageId: this.newLeadProductStageId })
      .subscribe({
        next: () => {
          this.newLeadProductId = '';
          this.newLeadProductQuantity = 1;
          this.newLeadProductStageId = '';
          this.savingLeadProduct.set(false);
          this.showLeadProductForm.set(false);
          this.toast.success('Product added.');
          this.loadLeadProducts();
          this.loadLead();
        },
        error: () => {
          this.savingLeadProduct.set(false);
          this.toast.error('Could not add this product.');
        },
      });
  }

  onProductStageSelectChange(value: string): void {
    if (value === '__new__') {
      this.showNewProductStageInput = true;
      this.newLeadProductStageId = '';
    } else {
      this.showNewProductStageInput = false;
      this.newLeadProductStageId = value;
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
        this.newLeadProductStageId = stage.id;
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

  updateLeadProductQuantity(item: LeadProduct, quantity: number): void {
    if (quantity < 1 || quantity === item.quantity) {
      return;
    }
    this.api.updateLeadProductQuantity(item.id, quantity).subscribe({
      next: () => {
        this.toast.success('Quantity updated.');
        this.loadLeadProducts();
        this.loadLead();
      },
      error: () => this.toast.error('Could not update the quantity.'),
    });
  }

  updateLeadProductStage(item: LeadProduct, productStageId: string): void {
    if (productStageId === item.productStageId) {
      return;
    }
    this.api.updateLeadProductStage(item.id, productStageId).subscribe({
      next: () => {
        this.toast.success('Status updated.');
        this.loadLeadProducts();
        this.loadActivities(); // backend logs a ProductStatusChange activity
      },
      error: () => this.toast.error('Could not update status.'),
    });
  }

  removeLeadProduct(item: LeadProduct): void {
    this.api.removeLeadProduct(item.id).subscribe({
      next: () => {
        this.toast.success('Product removed.');
        this.loadLeadProducts();
        this.loadLead();
      },
      error: () => this.toast.error('Could not remove this product.'),
    });
  }

  loadDocuments(): void {
    this.api.getLeadDocuments(this.leadId).subscribe({
      next: (items) => this.documents.set(items),
      error: () => this.errorMessage.set('Could not load documents.'),
    });
  }

  documentStatusClass(status: string): string {
    switch (status) {
      case 'Sent':
        return 'badge-primary';
      case 'Paid':
        return 'badge-success';
      default:
        return 'badge-secondary';
    }
  }

  createDocument(): void {
    this.savingDocument.set(true);
    const payload = { type: this.newDocumentType, taxRatePercent: this.newDocumentTaxRate, discountPercent: this.newDocumentDiscount };
    this.api.createDocument(this.leadId, payload).subscribe({
      next: () => {
        this.newDocumentType = 'Quotation';
        this.newDocumentTaxRate = 0;
        this.newDocumentDiscount = 0;
        this.savingDocument.set(false);
        this.showDocumentForm.set(false);
        this.toast.success('Document created.');
        this.loadDocuments();
        this.loadActivities();
      },
      error: (err) => {
        this.savingDocument.set(false);
        this.toast.error(err?.error?.message ?? 'Could not create this document.');
      },
    });
  }

  sendDocument(doc: LeadDocument): void {
    this.sendingDocumentId.set(doc.id);
    this.api.sendDocument(doc.id).subscribe({
      next: () => {
        this.sendingDocumentId.set(null);
        this.toast.success(`${doc.type} sent.`);
        this.loadDocuments();
        this.loadActivities();
      },
      error: (err) => {
        this.sendingDocumentId.set(null);
        this.toast.error(err?.error?.message ?? 'Could not send this document.');
      },
    });
  }

  markDocumentPaid(doc: LeadDocument): void {
    this.api.markDocumentPaid(doc.id).subscribe({
      next: () => {
        this.toast.success('Marked as paid.');
        this.loadDocuments();
        this.loadActivities();
      },
      error: (err) => this.toast.error(err?.error?.message ?? 'Could not mark this invoice as paid.'),
    });
  }

  downloadDocument(doc: LeadDocument): void {
    this.api.downloadDocument(doc.id).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${doc.number}.pdf`;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: () => this.toast.error('Could not download this document.'),
    });
  }

  loadMeetings(): void {
    this.api.getLeadMeetings(this.leadId).subscribe({
      next: (meetings) => this.meetings.set(meetings),
      error: () => this.errorMessage.set('Could not load meetings.'),
    });
  }

  loadCalendarConnection(): void {
    this.identityApi.getCalendarConnection().subscribe({
      next: (status) => this.calendarConnected.set(status.connected),
      // Non-fatal: an unconnected calendar is a valid, common state -- the meeting form
      // itself is never blocked by it. Treat a genuine lookup failure the same way.
      error: () => this.calendarConnected.set(false),
    });
  }

  connectCalendar(): void {
    const clientId = this.appConfig.googleClientId;
    if (!clientId) {
      this.toast.error('Google sign-in is not configured for this app.');
      return;
    }
    this.connectingCalendar.set(true);
    requestCalendarConnection(clientId)
      .then((code) => {
        this.identityApi.connectCalendar(code).subscribe({
          next: () => {
            this.connectingCalendar.set(false);
            this.toast.success('Google Calendar connected.');
            this.loadCalendarConnection();
          },
          error: () => {
            this.connectingCalendar.set(false);
            this.toast.error('Could not connect Google Calendar.');
          },
        });
      })
      .catch(() => {
        this.connectingCalendar.set(false);
      });
  }

  addMeeting(): void {
    if (!this.newMeetingTitle.trim() || !this.newMeetingStartLocal || !this.newMeetingEndLocal) {
      return;
    }
    // The datetime-local inputs give a value with no timezone (e.g. "2026-09-03T14:30");
    // Date treats that as local time, and toISOString() converts it to the UTC-everywhere
    // form the rest of this app already expects on the wire.
    const startAtUtc = new Date(this.newMeetingStartLocal).toISOString();
    const endAtUtc = new Date(this.newMeetingEndLocal).toISOString();

    this.savingMeeting.set(true);
    this.api
      .createMeeting(this.leadId, {
        title: this.newMeetingTitle.trim(),
        description: this.newMeetingDescription.trim() || undefined,
        startAtUtc,
        endAtUtc,
        location: this.newMeetingLocation.trim() || undefined,
        ownerUserId: this.newMeetingOwnerId || undefined,
      })
      .subscribe({
        next: () => {
          this.newMeetingTitle = '';
          this.newMeetingDescription = '';
          this.newMeetingStartLocal = '';
          this.newMeetingEndLocal = '';
          this.newMeetingLocation = '';
          this.newMeetingOwnerId = this.auth.currentUser()?.userId ?? '';
          this.savingMeeting.set(false);
          this.showMeetingForm.set(false);
          this.toast.success('Meeting scheduled.');
          this.loadMeetings();
        },
        error: () => {
          this.savingMeeting.set(false);
          this.toast.error('Could not schedule this meeting.');
        },
      });
  }

  cancelMeeting(meeting: Meeting): void {
    this.api.updateMeetingStatus(meeting.id, 'Cancelled').subscribe({
      next: () => {
        this.toast.success('Meeting cancelled.');
        this.loadMeetings();
      },
      error: () => this.toast.error('Could not cancel this meeting.'),
    });
  }

  completeMeeting(meeting: Meeting): void {
    this.api.updateMeetingStatus(meeting.id, 'Completed').subscribe({
      next: () => {
        this.toast.success('Meeting marked as completed.');
        this.loadMeetings();
      },
      error: () => this.toast.error('Could not update this meeting.'),
    });
  }

  meetingSyncLabel(meeting: Meeting): string {
    switch (meeting.googleSyncStatus) {
      case 'Synced':
        return 'Synced to Google Calendar';
      case 'Failed':
        return 'Sync failed';
      case 'ReauthRequired':
        return 'Reconnect Google Calendar to sync';
      default:
        return 'Not synced';
    }
  }

  meetingSyncClass(meeting: Meeting): string {
    switch (meeting.googleSyncStatus) {
      case 'Synced':
        return 'badge-success';
      case 'Failed':
        return 'badge-danger';
      case 'ReauthRequired':
        return 'badge-warning';
      default:
        return 'badge-secondary';
    }
  }

  meetingStatusClass(meeting: Meeting): string {
    switch (meeting.status) {
      case 'Completed':
        return 'badge-success';
      case 'Cancelled':
        return 'badge-secondary';
      default:
        return 'badge-primary';
    }
  }
}
