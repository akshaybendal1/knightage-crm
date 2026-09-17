import { Component, HostListener, OnInit, computed, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { CrmApi } from '../../core/crm-api';
import { IdentityApi } from '../../core/identity-api';
import {
  CustomReportDefinition,
  CustomReportRow,
  CustomReportViewType,
  OrgUser,
  PipelineStage,
  Product,
  ReportDimensionKey,
  ReportRange,
} from '../../core/models';
import { Toast } from '../../core/toast';
import { Loader } from '../../loader/loader';
import { ChartLine, ChartPoint } from '../reports/chart-line';
import { ChartBar } from '../reports/chart-bar';
import { ChartPie } from '../reports/chart-pie';

interface DimensionOption {
  key: ReportDimensionKey;
  label: string;
}

// Mirrors the backend's CustomReportDimensions whitelist exactly (Core/Interfaces/
// ICustomReportRepository.cs) -- not an open-ended field picker.
const DIMENSION_OPTIONS: DimensionOption[] = [
  { key: 'lead', label: 'Lead' },
  { key: 'stage', label: 'Pipeline stage' },
  { key: 'owner', label: 'Owner' },
  { key: 'product', label: 'Product' },
  { key: 'category', label: 'Product category' },
  { key: 'source', label: 'Source' },
  { key: 'createdDate', label: 'Created date' },
];

interface PivotTable {
  columns: string[];
  rows: { label: string; values: number[] }[];
}

@Component({
  selector: 'app-custom-report-builder',
  imports: [FormsModule, CurrencyPipe, RouterLink, Loader, ChartLine, ChartBar, ChartPie],
  templateUrl: './custom-report-builder.html',
  styleUrl: './custom-report-builder.css',
})
export class CustomReportBuilder implements OnInit {
  readonly dimensionOptions = DIMENSION_OPTIONS;
  readonly sourceOptions = ['Manual', 'Import'];

  reportId = signal<string | null>(null);
  // A signal (not a plain field like most other form fields in this codebase) --
  // saveDisabled below is a computed() that reads it, and computed() only
  // re-evaluates from tracked signal reads, not from plain-property mutation.
  name = signal('');

  selectedDimensions = signal<ReportDimensionKey[]>([]);
  viewType = signal<CustomReportViewType>('table');
  metric = signal<'leadCount' | 'totalValue'>('leadCount');

  range = signal<ReportRange>('week');
  customFrom = '';
  customTo = '';
  customRangeError = signal<string | null>(null);

  // Multi-select filters -- an empty array means "don't filter" (matches every value),
  // same as the single-value '' sentinel these replaced.
  selectedStageIds = signal<string[]>([]);
  selectedOwnerIds = signal<string[]>([]);
  selectedProductIds = signal<string[]>([]);
  selectedSources = signal<string[]>([]);

  // Which of the four filter dropdowns is open, if any -- only one at a time (matches
  // the app-wide top-bar user-menu dropdown convention: a signal + a document-click
  // HostListener that closes on any click outside .filter-dropdown, see app.ts).
  openFilterDropdown = signal<'stage' | 'product' | 'owner' | 'source' | null>(null);

  stages = signal<PipelineStage[]>([]);
  products = signal<Product[]>([]);
  orgUsers = signal<OrgUser[]>([]);

  stageFilterLabel = computed(() =>
    this.summaryLabel(this.selectedStageIds(), 'All stages', (id) => this.stages().find((s) => s.id === id)?.name),
  );
  productFilterLabel = computed(() =>
    this.summaryLabel(this.selectedProductIds(), 'All products', (id) => this.products().find((p) => p.id === id)?.name),
  );
  ownerFilterLabel = computed(() =>
    this.summaryLabel(this.selectedOwnerIds(), 'All owners', (id) => this.orgUsers().find((u) => u.id === id)?.displayName),
  );
  sourceFilterLabel = computed(() => this.summaryLabel(this.selectedSources(), 'All sources', (id) => id));

  rows = signal<CustomReportRow[]>([]);
  loading = signal(false);
  saving = signal(false);
  errorMessage = signal<string | null>(null);

  // A dimension/view-type combo the UI never lets the user reach directly (these back
  // the disabled state on the View-as buttons), but toggleDimension() can still make the
  // *current* selection invalid (e.g. adding a 2nd dimension while "bar" is active) --
  // reconcileViewType() resets to 'table' in that case rather than sending the backend a
  // combo it will 400 on.
  canUseLine = computed(() => this.selectedDimensions().includes('createdDate'));
  canUsePivot = computed(() => this.selectedDimensions().length === 2);
  canUseBarOrPie = computed(() => this.selectedDimensions().length === 1);

  saveDisabled = computed(() => this.selectedDimensions().length === 0 || !this.name().trim() || this.saving());

  chartPoints = computed<ChartPoint[]>(() =>
    this.rows().map((r) => ({ label: r.dim1Label, value: this.metric() === 'leadCount' ? r.leadCount : r.totalValue })),
  );

  pivot = computed<PivotTable | null>(() => {
    const rows = this.rows();
    if (this.viewType() !== 'pivot' || rows.length === 0) {
      return null;
    }
    const rowLabels: string[] = [];
    const colLabels: string[] = [];
    for (const r of rows) {
      if (!rowLabels.includes(r.dim1Label)) {
        rowLabels.push(r.dim1Label);
      }
      const colLabel = r.dim2Label ?? '—';
      if (!colLabels.includes(colLabel)) {
        colLabels.push(colLabel);
      }
    }
    const metric = this.metric();
    return {
      columns: colLabels,
      rows: rowLabels.map((rowLabel) => ({
        label: rowLabel,
        values: colLabels.map((colLabel) => {
          const match = rows.find((r) => r.dim1Label === rowLabel && (r.dim2Label ?? '—') === colLabel);
          return match ? (metric === 'leadCount' ? match.leadCount : match.totalValue) : 0;
        }),
      })),
    };
  });

  constructor(
    private readonly api: CrmApi,
    private readonly identityApi: IdentityApi,
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly toast: Toast,
  ) {}

  ngOnInit(): void {
    this.api.getPipelineStages().subscribe((stages) => this.stages.set(stages));
    this.api.getProducts().subscribe({
      next: (products) => this.products.set(products),
      error: () => {
        // Non-fatal: the product filter just stays empty.
      },
    });
    this.identityApi.getOrganizationUsers().subscribe({
      next: (users) => this.orgUsers.set(users),
      error: () => {
        // Non-fatal: the owner filter just stays empty.
      },
    });

    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.reportId.set(id);
      this.loadSaved(id);
    }
  }

  dimensionLabel(key: ReportDimensionKey): string {
    return this.dimensionOptions.find((d) => d.key === key)?.label ?? key;
  }

  private loadSaved(id: string): void {
    this.loading.set(true);
    this.api.runSavedCustomReport(id).subscribe({
      next: (result) => {
        this.name.set(result.name);
        this.hydrateFromDefinition(result.definition);
        this.rows.set(result.items);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load this report.');
        this.loading.set(false);
      },
    });
  }

  private hydrateFromDefinition(definition: CustomReportDefinition): void {
    this.selectedDimensions.set(definition.dimensions);
    this.viewType.set(definition.viewType);
    this.range.set(definition.range);
    this.customFrom = definition.from ?? '';
    this.customTo = definition.to ?? '';
    this.selectedStageIds.set(definition.stageIds ?? []);
    this.selectedOwnerIds.set(definition.ownerUserIds ?? []);
    this.selectedProductIds.set(definition.productIds ?? []);
    this.selectedSources.set(definition.sources ?? []);
  }

  // One generic toggle for all four multi-select filters -- same add/remove-from-array
  // shape regardless of which signal it's backing.
  private toggleInList(current: string[], value: string): string[] {
    return current.includes(value) ? current.filter((v) => v !== value) : [...current, value];
  }

  toggleStageFilter(id: string): void {
    this.selectedStageIds.update((current) => this.toggleInList(current, id));
    this.onFilterChange();
  }

  toggleOwnerFilter(id: string): void {
    this.selectedOwnerIds.update((current) => this.toggleInList(current, id));
    this.onFilterChange();
  }

  toggleProductFilter(id: string): void {
    this.selectedProductIds.update((current) => this.toggleInList(current, id));
    this.onFilterChange();
  }

  toggleSourceFilter(value: string): void {
    this.selectedSources.update((current) => this.toggleInList(current, value));
    this.onFilterChange();
  }

  private summaryLabel(selected: string[], allLabel: string, resolveLabel: (id: string) => string | undefined): string {
    if (selected.length === 0) {
      return allLabel;
    }
    if (selected.length === 1) {
      return resolveLabel(selected[0]) ?? '1 selected';
    }
    return `${selected.length} selected`;
  }

  toggleFilterDropdown(key: 'stage' | 'product' | 'owner' | 'source', event: Event): void {
    event.stopPropagation();
    this.openFilterDropdown.update((current) => (current === key ? null : key));
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    if (this.openFilterDropdown() && !target.closest('.filter-dropdown')) {
      this.openFilterDropdown.set(null);
    }
  }

  toggleDimension(key: ReportDimensionKey): void {
    this.selectedDimensions.update((current) => {
      if (current.includes(key)) {
        return current.filter((d) => d !== key);
      }
      if (current.length >= 2) {
        this.toast.error('A report can use at most 2 dimensions.');
        return current;
      }
      return [...current, key];
    });
    this.reconcileViewType();
    this.run();
  }

  private reconcileViewType(): void {
    if (this.viewType() === 'line' && !this.canUseLine()) {
      this.viewType.set('table');
    }
    if (this.viewType() === 'pivot' && !this.canUsePivot()) {
      this.viewType.set('table');
    }
    if ((this.viewType() === 'bar' || this.viewType() === 'pie') && !this.canUseBarOrPie()) {
      this.viewType.set('table');
    }
  }

  setViewType(type: CustomReportViewType): void {
    // Presentation-only -- the same rows() already loaded are just rendered differently,
    // no re-fetch needed (viewType only affects backend-side validation, never the data
    // a run actually returns).
    this.viewType.set(type);
  }

  setMetric(metric: 'leadCount' | 'totalValue'): void {
    this.metric.set(metric);
  }

  setRange(range: ReportRange): void {
    this.range.set(range);
    this.customRangeError.set(null);
    if (range !== 'custom') {
      this.run();
    }
  }

  applyCustomRange(): void {
    if (!this.customFrom || !this.customTo) {
      this.customRangeError.set('Pick both a start and end date.');
      return;
    }
    if (this.customFrom > this.customTo) {
      this.customRangeError.set('Start date must be on or before the end date.');
      return;
    }
    this.customRangeError.set(null);
    this.run();
  }

  onFilterChange(): void {
    this.run();
  }

  private buildDefinition(): CustomReportDefinition | null {
    const dimensions = this.selectedDimensions();
    if (dimensions.length === 0) {
      return null;
    }
    return {
      dimensions,
      viewType: this.viewType(),
      range: this.range(),
      from: this.range() === 'custom' ? this.customFrom || null : null,
      to: this.range() === 'custom' ? this.customTo || null : null,
      stageIds: this.selectedStageIds().length ? this.selectedStageIds() : null,
      ownerUserIds: this.selectedOwnerIds().length ? this.selectedOwnerIds() : null,
      productIds: this.selectedProductIds().length ? this.selectedProductIds() : null,
      sources: this.selectedSources().length ? this.selectedSources() : null,
    };
  }

  run(): void {
    const definition = this.buildDefinition();
    if (!definition) {
      this.rows.set([]);
      return;
    }
    // Custom range only actually reloads once both dates are set and Apply is clicked --
    // matches reports.ts's own applyCustomRange() pattern exactly.
    if (definition.range === 'custom' && (!definition.from || !definition.to)) {
      return;
    }

    this.loading.set(true);
    this.errorMessage.set(null);
    this.api.runCustomReport(definition).subscribe({
      next: (result) => {
        this.rows.set(result.items);
        this.loading.set(false);
      },
      error: (err) => {
        this.errorMessage.set(err?.error?.message ?? 'Could not run this report.');
        this.loading.set(false);
      },
    });
  }

  save(): void {
    const definition = this.buildDefinition();
    const trimmedName = this.name().trim();
    if (!definition || !trimmedName) {
      return;
    }

    this.saving.set(true);
    const id = this.reportId();
    const request = id ? this.api.updateCustomReport(id, trimmedName, definition) : this.api.saveCustomReport(trimmedName, definition);
    request.subscribe({
      next: (result) => {
        this.saving.set(false);
        this.toast.success('Report saved.');
        if (!id) {
          this.router.navigate(['/custom-reports', result.id]);
        }
      },
      error: () => {
        this.saving.set(false);
        this.toast.error('Could not save this report.');
      },
    });
  }

  exportReport(): void {
    const definition = this.buildDefinition();
    if (!definition) {
      return;
    }
    this.api.exportCustomReport(definition).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `custom-report-${new Date().toISOString().slice(0, 10)}.xlsx`;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: () => this.toast.error('Could not export this report.'),
    });
  }
}
