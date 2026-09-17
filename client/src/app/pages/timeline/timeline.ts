import { Component, OnInit, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CdkDragDrop, DragDropModule, transferArrayItem } from '@angular/cdk/drag-drop';
import { Auth } from '../../core/auth';
import { CrmApi } from '../../core/crm-api';
import { IdentityApi } from '../../core/identity-api';
import { LeadProductTimelineItem, LeadTimelineItem, OrgUser, PipelineStage, Product, ProductStage } from '../../core/models';
import { STUCK_THRESHOLD_DAYS } from '../../core/stuck-status';
import { Toast } from '../../core/toast';
import { Loader } from '../../loader/loader';

type TimelineView = 'leads' | 'products';
const PAGE_SIZE = 15;

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
  imports: [RouterLink, DragDropModule, Loader, CurrencyPipe, FormsModule],
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
  // the drop; this just persists it server-side and reverts on failure.
  onLeadDrop(event: CdkDragDrop<LeadTimelineItem[]>, targetColumn: LeadColumn): void {
    if (event.previousContainer === event.container) {
      return;
    }
    const item = event.previousContainer.data[event.previousIndex];
    transferArrayItem(event.previousContainer.data, event.container.data, event.previousIndex, event.currentIndex);
    this.leadColumns.update((cols) => [...cols]);

    this.api.updateLeadStage(item.leadId, targetColumn.stage.id).subscribe({
      next: () => {
        this.leadColumns.update((cols) =>
          cols.map((c) => {
            if (c === targetColumn) return { ...c, totalCount: c.totalCount + 1 };
            if (c.items === event.previousContainer.data) return { ...c, totalCount: Math.max(0, c.totalCount - 1) };
            return c;
          }),
        );
        this.toast.success(`Moved to ${targetColumn.stage.name}.`);
      },
      error: () => {
        transferArrayItem(event.container.data, event.previousContainer.data, event.currentIndex, event.previousIndex);
        this.leadColumns.update((cols) => [...cols]);
        this.toast.error('Could not move this lead.');
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

    this.api.updateLeadProductStage(item.leadProductId, targetColumn.stage.id).subscribe({
      next: () => {
        this.productColumns.update((cols) =>
          cols.map((c) => {
            if (c === targetColumn) return { ...c, totalCount: c.totalCount + 1 };
            if (c.items === event.previousContainer.data) return { ...c, totalCount: Math.max(0, c.totalCount - 1) };
            return c;
          }),
        );
        this.toast.success(`Moved to ${targetColumn.stage.name}.`);
      },
      error: () => {
        transferArrayItem(event.container.data, event.previousContainer.data, event.currentIndex, event.previousIndex);
        this.productColumns.update((cols) => [...cols]);
        this.toast.error('Could not move this product.');
      },
    });
  }
}
