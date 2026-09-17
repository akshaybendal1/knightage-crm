import { Component, OnInit, computed, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { CrmApi } from '../../core/crm-api';
import { IdentityApi } from '../../core/identity-api';
import { Auth } from '../../core/auth';
import { Toast } from '../../core/toast';
import { RouterLink } from '@angular/router';
import {
  CustomRange,
  LeadSourceBreakdownItem,
  LeadSourceDetailItem,
  LeadsByCategoryItem,
  LeadsByOwnerRow,
  LeadsByProductItem,
  LeadTrendPoint,
  OrgUser,
  ReportExportKey,
  ReportRange,
  SavedCustomReportSummary,
  TaskTrendPoint,
  TeamPerformanceRow,
  WonLostSummary,
} from '../../core/models';
import { ChartLine, ChartPoint } from './chart-line';
import { Loader } from '../../loader/loader';

@Component({
  selector: 'app-reports',
  imports: [ChartLine, Loader, DatePipe, CurrencyPipe, FormsModule, RouterLink],
  templateUrl: './reports.html',
})
export class Reports implements OnInit {
  range = signal<ReportRange>('week');
  errorMessage = signal<string | null>(null);

  // Custom range: two plain yyyy-MM-dd date inputs. Reports only actually
  // reload once both are set and "Apply" is clicked -- reloading on every
  // keystroke/partial pick would fire a request per section for a
  // half-typed date.
  customFrom = '';
  customTo = '';
  customRangeError = signal<string | null>(null);

  // Section 1: Lead volume & conversion -- two independent requests (a new trend
  // endpoint, and the existing won/lost endpoint reused as-is), both range-scoped.
  leadTrend = signal<LeadTrendPoint[]>([]);
  wonLost = signal<WonLostSummary | null>(null);
  private leadTrendLoaded = signal(false);
  private wonLostLoaded = signal(false);
  leadVolumeLoading = computed(() => !(this.leadTrendLoaded() && this.wonLostLoaded()));

  // Section 2: Lead source breakdown, plus a click-to-drill-down detail list.
  leadSources = signal<LeadSourceBreakdownItem[]>([]);
  leadSourcesLoading = signal(false);
  selectedSource = signal<string | null>(null);
  sourceDetails = signal<LeadSourceDetailItem[]>([]);
  sourceDetailsLoading = signal(false);
  orgUsers = signal<OrgUser[]>([]);

  // Section 3: Team performance.
  teamPerformance = signal<TeamPerformanceRow[]>([]);
  teamPerformanceLoading = signal(false);

  // Section 4: Task completion & overdue -- a range-scoped trend plus the existing
  // overdue-count endpoint, which (matching Dashboard's own card) is a current-state
  // snapshot and isn't range-filtered, so it only loads once rather than on every
  // range change.
  taskTrend = signal<TaskTrendPoint[]>([]);
  overdueTaskCount = signal<number | null>(null);
  private taskTrendLoaded = signal(false);
  private overdueCountLoaded = signal(false);
  taskCompletionLoading = computed(() => !(this.taskTrendLoaded() && this.overdueCountLoaded()));

  // Section 5 & 6: Leads by product, leads by owner.
  leadsByProduct = signal<LeadsByProductItem[]>([]);
  leadsByProductLoading = signal(false);
  leadsByCategory = signal<LeadsByCategoryItem[]>([]);
  leadsByCategoryLoading = signal(false);
  leadsByOwner = signal<LeadsByOwnerRow[]>([]);
  leadsByOwnerLoading = signal(false);

  // Custom reports: not range-scoped like the sections above -- each saved report
  // carries its own range/filters, this is just a list of what's saved.
  savedReports = signal<SavedCustomReportSummary[]>([]);
  savedReportsLoading = signal(false);

  conversionRate = computed(() => {
    const wl = this.wonLost();
    if (!wl || wl.won + wl.lost === 0) {
      return null;
    }
    return Math.round((wl.won / (wl.won + wl.lost)) * 100);
  });

  leadTrendPoints = computed<ChartPoint[]>(() =>
    this.leadTrend().map((p) => ({ label: this.formatBucketLabel(p.bucketStart), value: p.leadCount })),
  );

  taskTrendPoints = computed<ChartPoint[]>(() =>
    this.taskTrend().map((p) => ({ label: this.formatBucketLabel(p.bucketStart), value: p.completedCount })),
  );

  constructor(
    private readonly api: CrmApi,
    private readonly identityApi: IdentityApi,
    protected readonly auth: Auth,
    private readonly toast: Toast,
  ) {}

  ngOnInit(): void {
    this.loadAll();
    this.loadOverdueCount();
    this.loadSavedReports();
    this.identityApi.getOrganizationUsers().subscribe({
      next: (users) => this.orgUsers.set(users),
      error: () => {
        // Non-fatal: the drill-down's Owner column just falls back to a shortened id.
      },
    });
  }

  setRange(range: ReportRange): void {
    this.range.set(range);
    this.selectedSource.set(null);
    this.customRangeError.set(null);
    // 'custom' just reveals the from/to inputs -- actually reloading waits for
    // applyCustomRange(), so a half-picked date never fires a request per section.
    if (range !== 'custom') {
      this.loadAll();
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
    this.selectedSource.set(null);
    this.loadAll();
  }

  private currentCustomRange(): CustomRange | undefined {
    return this.range() === 'custom' && this.customFrom && this.customTo
      ? { from: this.customFrom, to: this.customTo }
      : undefined;
  }

  maxSourceCount(): number {
    return Math.max(1, ...this.leadSources().map((s) => s.leadCount));
  }

  sourceBarWidth(count: number): number {
    return Math.round((count / this.maxSourceCount()) * 100);
  }

  selectSource(source: string): void {
    if (this.selectedSource() === source) {
      this.selectedSource.set(null);
      return;
    }
    this.selectedSource.set(source);
    this.sourceDetailsLoading.set(true);
    this.api.getLeadSourceDetail(source, this.range(), this.currentCustomRange()).subscribe({
      next: (items) => {
        this.sourceDetails.set(items);
        this.sourceDetailsLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load leads for this source.');
        this.sourceDetailsLoading.set(false);
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

  exportReport(key: ReportExportKey, filePrefix: string): void {
    this.api.exportReport(key, this.range(), this.currentCustomRange()).subscribe({
      next: (blob) => {
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `${filePrefix}-${this.range()}.xlsx`;
        a.click();
        URL.revokeObjectURL(url);
      },
      error: () => this.errorMessage.set('Could not export this report.'),
    });
  }

  private loadAll(): void {
    this.loadLeadTrend();
    this.loadWonLost();
    this.loadLeadSources();
    this.loadTeamPerformance();
    this.loadTaskTrend();
    this.loadLeadsByProduct();
    this.loadLeadsByCategory();
    this.loadLeadsByOwner();
  }

  private loadLeadTrend(): void {
    this.api.getLeadTrends(this.range(), this.currentCustomRange()).subscribe({
      next: (points) => {
        this.leadTrend.set(points);
        this.leadTrendLoaded.set(true);
      },
      error: () => {
        this.errorMessage.set('Could not load the lead volume trend.');
        this.leadTrendLoaded.set(true);
      },
    });
  }

  private loadWonLost(): void {
    this.api.getWonLostSummary(this.range(), this.currentCustomRange()).subscribe({
      next: (summary) => {
        this.wonLost.set(summary);
        this.wonLostLoaded.set(true);
      },
      error: () => {
        this.errorMessage.set('Could not load the won/lost summary.');
        this.wonLostLoaded.set(true);
      },
    });
  }

  private loadLeadSources(): void {
    this.leadSourcesLoading.set(true);
    this.api.getLeadSourceBreakdown(this.range(), this.currentCustomRange()).subscribe({
      next: (items) => {
        this.leadSources.set(items);
        this.leadSourcesLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load the lead source breakdown.');
        this.leadSourcesLoading.set(false);
      },
    });
  }

  private loadTeamPerformance(): void {
    this.teamPerformanceLoading.set(true);
    this.api.getTeamPerformance(this.range(), this.currentCustomRange()).subscribe({
      next: (rows) => {
        this.teamPerformance.set(rows);
        this.teamPerformanceLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load team performance.');
        this.teamPerformanceLoading.set(false);
      },
    });
  }

  private loadTaskTrend(): void {
    this.api.getTaskCompletionTrend(this.range(), this.currentCustomRange()).subscribe({
      next: (points) => {
        this.taskTrend.set(points);
        this.taskTrendLoaded.set(true);
      },
      error: () => {
        this.errorMessage.set('Could not load the task completion trend.');
        this.taskTrendLoaded.set(true);
      },
    });
  }

  private loadLeadsByProduct(): void {
    this.leadsByProductLoading.set(true);
    this.api.getLeadsByProduct(this.range(), this.currentCustomRange()).subscribe({
      next: (items) => {
        this.leadsByProduct.set(items);
        this.leadsByProductLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load leads by product.');
        this.leadsByProductLoading.set(false);
      },
    });
  }

  private loadLeadsByCategory(): void {
    this.leadsByCategoryLoading.set(true);
    this.api.getLeadsByCategory(this.range(), this.currentCustomRange()).subscribe({
      next: (items) => {
        this.leadsByCategory.set(items);
        this.leadsByCategoryLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load leads by category.');
        this.leadsByCategoryLoading.set(false);
      },
    });
  }

  private loadLeadsByOwner(): void {
    this.leadsByOwnerLoading.set(true);
    this.api.getLeadsByOwner(this.range(), this.currentCustomRange()).subscribe({
      next: (rows) => {
        this.leadsByOwner.set(rows);
        this.leadsByOwnerLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load leads by owner.');
        this.leadsByOwnerLoading.set(false);
      },
    });
  }

  private loadSavedReports(): void {
    this.savedReportsLoading.set(true);
    this.api.getSavedCustomReports().subscribe({
      next: (reports) => {
        this.savedReports.set(reports);
        this.savedReportsLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load saved custom reports.');
        this.savedReportsLoading.set(false);
      },
    });
  }

  deleteSavedReport(report: SavedCustomReportSummary): void {
    if (!confirm(`Delete "${report.name}"? This can't be undone from here.`)) {
      return;
    }
    this.api.deleteCustomReport(report.id).subscribe({
      next: () => {
        this.toast.success('Report deleted.');
        this.loadSavedReports();
      },
      error: () => this.errorMessage.set('Could not delete this report.'),
    });
  }

  private loadOverdueCount(): void {
    this.api.getOverdueTaskCount().subscribe({
      next: (result) => {
        this.overdueTaskCount.set(result.count);
        this.overdueCountLoaded.set(true);
      },
      error: () => {
        this.errorMessage.set('Could not load overdue task count.');
        this.overdueCountLoaded.set(true);
      },
    });
  }

  private formatBucketLabel(bucketStart: string): string {
    const date = new Date(bucketStart);
    // Mirrors the backend's own bucketByMonth decision (ResolveRange in
    // ReportsController): 'all', or a custom range spanning more than ~60
    // days, buckets by month server-side -- match that here so the label
    // format (month-only vs month+day) matches what the data actually is.
    const useMonthFormat = this.range() === 'all' || this.customRangeSpanDays() > 60;
    return useMonthFormat
      ? date.toLocaleDateString(undefined, { month: 'short', year: '2-digit' })
      : date.toLocaleDateString(undefined, { month: 'short', day: 'numeric' });
  }

  private customRangeSpanDays(): number {
    if (this.range() !== 'custom' || !this.customFrom || !this.customTo) {
      return 0;
    }
    const msPerDay = 1000 * 60 * 60 * 24;
    return (new Date(this.customTo).getTime() - new Date(this.customFrom).getTime()) / msPerDay;
  }
}
