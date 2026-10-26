import { Component, OnInit, computed, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CdkDragDrop, DragDropModule, transferArrayItem } from '@angular/cdk/drag-drop';
import { Observable } from 'rxjs';
import { Auth } from '../../core/auth';
import { CrmApi } from '../../core/crm-api';
import { IdentityApi } from '../../core/identity-api';
import { BulkStageMoveResponse, LeadProductTimelineItem, LeadTimelineItem, OrgUser, PipelineStage, Product, ProductStage } from '../../core/models';
import { STUCK_THRESHOLD_DAYS } from '../../core/stuck-status';
import { describeBulkMove } from '../../core/stage-move';
import { Toast } from '../../core/toast';
import { Loader } from '../../loader/loader';
import { StageMoveDialog, StageMoveDialogItem, StageMoveDialogResult } from '../../stage-move-dialog/stage-move-dialog';

type TimelineView = 'leads' | 'products';
const PAGE_SIZE = 15;

// What the note dialog is currently asking about, and what to do with the answer. A drag-drop
// and a bulk move both use it; only the callbacks differ.
interface MoveDialogState {
  noun: 'lead' | 'product';
  stageName: string;
  items: StageMoveDialogItem[];
  onConfirm: (result: StageMoveDialogResult) => void;
  onCancel: () => void;
}

interface LeadColumn {
  stage: PipelineStage;
  items: LeadTimelineItem[];
  totalCount: number;
  page: number;
  loading: boolean;
}

interface ProductColumn {
  stage: ProductStage;
  items: LeadProductTimelineItem[];
  totalCount: number;
  page: number;
  loading: boolean;
}

@Component({
  selector: 'app-timeline',
  imports: [RouterLink, DragDropModule, Loader, CurrencyPipe, FormsModule, StageMoveDialog],
  templateUrl: './timeline.html',
})
export class Timeline implements OnInit {
  view = signal<TimelineView>('leads');
  leadColumns = signal<LeadColumn[]>([]);
  productColumns = signal<ProductColumn[]>([]);
  loading = signal(true);
  errorMessage = signal<string | null>(null);
  orgUsers = signal<OrgUser[]>([]);
  products = signal<Product[]>([]);

  // Note dialog shown before a move is saved (drag-drop or bulk).
  moveDialog = signal<MoveDialogState | null>(null);
  movingBusy = signal(false);

  // Bulk selection. The whole item is kept (not just its id) so the dialog can still name
  // a card after the column it was in has been reloaded.
  selectMode = signal(false);
  selectedLeads = signal<Map<string, LeadTimelineItem>>(new Map());
  selectedProducts = signal<Map<string, LeadProductTimelineItem>>(new Map());
  bulkLeadStageId = '';
  bulkProductStageId = '';
  selectedCount = computed(() => (this.view() === 'leads' ? this.selectedLeads().size : this.selectedProducts().size));

  filterProductId = '';
  filterOwnerId = '';
  filterSearch = '';
  private searchDebounceTimer: ReturnType<typeof setTimeout> | null = null;

  readonly stuckThresholdDays = STUCK_THRESHOLD_DAYS;

  constructor(
    private readonly api: CrmApi,
    private readonly identityApi: IdentityApi,
    private readonly toast: Toast,
    protected readonly auth: Auth,
  ) {}

  ngOnInit(): void {
    this.identityApi.getOrganizationUsers().subscribe({
      next: (users) => this.orgUsers.set(users),
      error: () => {
        // Non-fatal: owner labels just fall back to a short id.
      },
    });
    this.api.getPipelineStages().subscribe({
      next: (stages) => {
        const sorted = stages.slice().sort((a, b) => a.sortOrder - b.sortOrder);
        this.leadColumns.set(sorted.map((stage) => ({ stage, items: [], totalCount: 0, page: 0, loading: false })));
        this.leadColumns().forEach((_, index) => this.loadLeadColumnPage(index));
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load pipeline stages.');
        this.loading.set(false);
      },
    });
    this.api.getProductStages().subscribe({
      next: (stages) => {
        const sorted = stages.slice().sort((a, b) => a.sortOrder - b.sortOrder);
        this.productColumns.set(sorted.map((stage) => ({ stage, items: [], totalCount: 0, page: 0, loading: false })));
        this.productColumns().forEach((_, index) => this.loadProductColumnPage(index));
      },
      error: () => this.errorMessage.set('Could not load product stages.'),
    });
    this.api.getProducts().subscribe({
      next: (products) => this.products.set(products.filter((p) => p.isActive)),
      error: () => {
        // Non-fatal: the product filter dropdown just stays empty.
      },
    });
  }

  setView(view: TimelineView): void {
    this.view.set(view);
    this.clearSelection();
  }

  // Resets every column back to empty/page-0 and reloads page 1 -- a filter change
  // invalidates whatever's already accumulated in each column, same as the Leads
  // page resetting to page 1 on a filter change.
  onFilterChange(): void {
    this.leadColumns.update((cols) => cols.map((c) => ({ ...c, items: [], totalCount: 0, page: 0 })));
    this.leadColumns().forEach((_, index) => this.loadLeadColumnPage(index));
    this.productColumns.update((cols) => cols.map((c) => ({ ...c, items: [], totalCount: 0, page: 0 })));
    this.productColumns().forEach((_, index) => this.loadProductColumnPage(index));
  }

  // Debounced so typing a lead-name search doesn't fire a request per keystroke.
  onSearchChange(): void {
    if (this.searchDebounceTimer) {
      clearTimeout(this.searchDebounceTimer);
    }
    this.searchDebounceTimer = setTimeout(() => this.onFilterChange(), 300);
  }

  // A Won/Lost stage's own items are never "stuck" -- they're closed, not sitting
  // waiting -- regardless of how many days have passed since they landed there.
  isStuck(days: number, stage: { isWon: boolean; isLost: boolean }): boolean {
    return days >= this.stuckThresholdDays && !stage.isWon && !stage.isLost;
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

  private updateLeadColumn(index: number, patch: Partial<LeadColumn>): void {
    this.leadColumns.update((cols) => cols.map((c, i) => (i === index ? { ...c, ...patch } : c)));
  }

  private updateProductColumn(index: number, patch: Partial<ProductColumn>): void {
    this.productColumns.update((cols) => cols.map((c, i) => (i === index ? { ...c, ...patch } : c)));
  }

  loadLeadColumnPage(index: number): void {
    const col = this.leadColumns()[index];
    if (!col || col.loading) {
      return;
    }
    const nextPage = col.page + 1;
    this.updateLeadColumn(index, { loading: true });
    const productId = this.filterProductId || undefined;
    const ownerUserId = this.filterOwnerId || undefined;
    const search = this.filterSearch.trim() || undefined;
    this.api.getLeadsTimeline(col.stage.id, productId, ownerUserId, search, nextPage, PAGE_SIZE).subscribe({
      next: (result) => {
        const current = this.leadColumns()[index];
        this.updateLeadColumn(index, {
          items: [...current.items, ...result.items],
          totalCount: result.totalCount,
          page: nextPage,
          loading: false,
        });
      },
      error: () => {
        this.updateLeadColumn(index, { loading: false });
        this.errorMessage.set('Could not load the leads timeline.');
      },
    });
  }

  loadProductColumnPage(index: number): void {
    const col = this.productColumns()[index];
    if (!col || col.loading) {
      return;
    }
    const nextPage = col.page + 1;
    this.updateProductColumn(index, { loading: true });
    const productId = this.filterProductId || undefined;
    const ownerUserId = this.filterOwnerId || undefined;
    const search = this.filterSearch.trim() || undefined;
    this.api.getLeadProductsTimeline(col.stage.id, productId, ownerUserId, search, nextPage, PAGE_SIZE).subscribe({
      next: (result) => {
        const current = this.productColumns()[index];
        this.updateProductColumn(index, {
          items: [...current.items, ...result.items],
          totalCount: result.totalCount,
          page: nextPage,
          loading: false,
        });
      },
      error: () => {
        this.updateProductColumn(index, { loading: false });
        this.errorMessage.set('Could not load the lead products timeline.');
      },
    });
  }

  // Optimistic move: CDK already visually transfers the card between columns as part of
  // the drop. Nothing is saved yet -- a dialog asks for an optional note first. Confirming
  // persists the move (and puts the card back if that fails); cancelling puts the card back.
  onLeadDrop(event: CdkDragDrop<LeadTimelineItem[]>, targetColumn: LeadColumn): void {
    if (event.previousContainer === event.container) {
      return;
    }
    const item = event.previousContainer.data[event.previousIndex];
    transferArrayItem(event.previousContainer.data, event.container.data, event.previousIndex, event.currentIndex);
    this.leadColumns.update((cols) => [...cols]);

    const revert = (): void => {
      transferArrayItem(event.container.data, event.previousContainer.data, event.currentIndex, event.previousIndex);
      this.leadColumns.update((cols) => [...cols]);
      this.moveDialog.set(null);
    };

    this.moveDialog.set({
      noun: 'lead',
      stageName: targetColumn.stage.name,
      items: [{ id: item.leadId, label: item.leadName, sublabel: item.company }],
      onCancel: revert,
      onConfirm: (result) => {
        this.movingBusy.set(true);
        this.api.updateLeadStage(item.leadId, targetColumn.stage.id, result.items[0]?.note).subscribe({
          next: () => {
            this.leadColumns.update((cols) =>
              cols.map((c) => {
                if (c === targetColumn) return { ...c, totalCount: c.totalCount + 1 };
                if (c.items === event.previousContainer.data) return { ...c, totalCount: Math.max(0, c.totalCount - 1) };
                return c;
              }),
            );
            this.movingBusy.set(false);
            this.moveDialog.set(null);
            this.toast.success(`Moved to ${targetColumn.stage.name}.`);
          },
          error: () => {
            this.movingBusy.set(false);
            revert();
            this.toast.error('Could not move this lead.');
          },
        });
      },
    });
  }

  onProductDrop(event: CdkDragDrop<LeadProductTimelineItem[]>, targetColumn: ProductColumn): void {
    if (event.previousContainer === event.container) {
      return;
    }
    const item = event.previousContainer.data[event.previousIndex];
    transferArrayItem(event.previousContainer.data, event.container.data, event.previousIndex, event.currentIndex);
    this.productColumns.update((cols) => [...cols]);

    const revert = (): void => {
      transferArrayItem(event.container.data, event.previousContainer.data, event.currentIndex, event.previousIndex);
      this.productColumns.update((cols) => [...cols]);
      this.moveDialog.set(null);
    };

    this.moveDialog.set({
      noun: 'product',
      stageName: targetColumn.stage.name,
      items: [{ id: item.leadProductId, label: `${item.productName || 'Product'} \u00b7 ${item.leadName}` }],
      onCancel: revert,
      onConfirm: (result) => {
        this.movingBusy.set(true);
        this.api.updateLeadProductStage(item.leadProductId, targetColumn.stage.id, result.items[0]?.note).subscribe({
          next: () => {
            this.productColumns.update((cols) =>
              cols.map((c) => {
                if (c === targetColumn) return { ...c, totalCount: c.totalCount + 1 };
                if (c.items === event.previousContainer.data) return { ...c, totalCount: Math.max(0, c.totalCount - 1) };
                return c;
              }),
            );
            this.movingBusy.set(false);
            this.moveDialog.set(null);
            this.toast.success(`Moved to ${targetColumn.stage.name}.`);
          },
          error: () => {
            this.movingBusy.set(false);
            revert();
            this.toast.error('Could not move this product.');
          },
        });
      },
    });
  }

  // ---- Bulk selection and move ----

  toggleSelectMode(): void {
    this.selectMode.update((on) => !on);
    if (!this.selectMode()) {
      this.clearSelection();
    }
  }

  clearSelection(): void {
    this.selectedLeads.set(new Map());
    this.selectedProducts.set(new Map());
    this.bulkLeadStageId = '';
    this.bulkProductStageId = '';
  }

  isLeadSelected(id: string): boolean {
    return this.selectedLeads().has(id);
  }

  toggleLeadSelected(item: LeadTimelineItem): void {
    const next = new Map(this.selectedLeads());
    if (!next.delete(item.leadId)) {
      next.set(item.leadId, item);
    }
    this.selectedLeads.set(next);
  }

  leadColumnAllSelected(column: LeadColumn): boolean {
    return column.items.length > 0 && column.items.every((i) => this.selectedLeads().has(i.leadId));
  }

  toggleLeadColumn(column: LeadColumn): void {
    const next = new Map(this.selectedLeads());
    if (this.leadColumnAllSelected(column)) {
      column.items.forEach((i) => next.delete(i.leadId));
    } else {
      column.items.forEach((i) => next.set(i.leadId, i));
    }
    this.selectedLeads.set(next);
  }

  isProductSelected(id: string): boolean {
    return this.selectedProducts().has(id);
  }

  toggleProductSelected(item: LeadProductTimelineItem): void {
    const next = new Map(this.selectedProducts());
    if (!next.delete(item.leadProductId)) {
      next.set(item.leadProductId, item);
    }
    this.selectedProducts.set(next);
  }

  productColumnAllSelected(column: ProductColumn): boolean {
    return column.items.length > 0 && column.items.every((i) => this.selectedProducts().has(i.leadProductId));
  }

  toggleProductColumn(column: ProductColumn): void {
    const next = new Map(this.selectedProducts());
    if (this.productColumnAllSelected(column)) {
      column.items.forEach((i) => next.delete(i.leadProductId));
    } else {
      column.items.forEach((i) => next.set(i.leadProductId, i));
    }
    this.selectedProducts.set(next);
  }

  startBulkMove(): void {
    if (this.view() === 'leads') {
      const stage = this.leadColumns().find((c) => c.stage.id === this.bulkLeadStageId)?.stage;
      const items = [...this.selectedLeads().values()];
      if (!stage || items.length === 0) {
        return;
      }
      this.openBulkDialog(
        'lead',
        stage.name,
        items.map((i) => ({ id: i.leadId, label: i.leadName, sublabel: i.company })),
        (result) => this.api.bulkUpdateLeadStage(stage.id, result.items.map((i) => ({ id: i.id, note: i.note || undefined }))),
      );
    } else {
      const stage = this.productColumns().find((c) => c.stage.id === this.bulkProductStageId)?.stage;
      const items = [...this.selectedProducts().values()];
      if (!stage || items.length === 0) {
        return;
      }
      this.openBulkDialog(
        'product',
        stage.name,
        items.map((i) => ({ id: i.leadProductId, label: i.productName || 'Product', sublabel: i.leadName })),
        (result) => this.api.bulkUpdateLeadProductStage(stage.id, result.items.map((i) => ({ id: i.id, note: i.note || undefined }))),
      );
    }
  }

  private openBulkDialog(
    noun: 'lead' | 'product',
    stageName: string,
    items: StageMoveDialogItem[],
    send: (result: StageMoveDialogResult) => Observable<BulkStageMoveResponse>,
  ): void {
    this.moveDialog.set({
      noun,
      stageName,
      items,
      onCancel: () => this.moveDialog.set(null),
      onConfirm: (result) => {
        this.movingBusy.set(true);
        send(result).subscribe({
          next: (res) => {
            this.movingBusy.set(false);
            this.moveDialog.set(null);
            this.reportBulkResult(res, stageName);
            this.clearSelection();
            // Columns changed in ways a local patch can't track (counts, order, new pages).
            this.onFilterChange();
          },
          error: () => {
            this.movingBusy.set(false);
            this.toast.error(`Could not move the selected ${noun}s. Nothing was changed that you can't see here -- refresh and check.`);
          },
        });
      },
    });
  }

  private reportBulkResult(res: BulkStageMoveResponse, stageName: string): void {
    const summary = describeBulkMove(res, stageName);
    if (summary.hasFailures) {
      this.toast.error(summary.text);
    } else {
      this.toast.success(summary.text);
    }
  }
}
