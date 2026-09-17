import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePipe, DecimalPipe } from '@angular/common';
import { Auth } from '../../core/auth';
import { CrmApi } from '../../core/crm-api';
import { IdentityApi } from '../../core/identity-api';
import { ChartBar } from '../reports/chart-bar';
import { ChartPoint } from '../reports/chart-line';
import { CustomRange, FieldSalesEntryItem, OrgUser, ReportRange, SalesByRepRow } from '../../core/models';
import { Loader } from '../../loader/loader';

type SalesByRepMetric = 'count' | 'value';

@Component({
  selector: 'app-sales-dashboard',
  imports: [FormsModule, DatePipe, DecimalPipe, Loader, ChartBar],
  templateUrl: './sales-dashboard.html',
  styleUrl: './sales-dashboard.css',
})
export class SalesDashboard implements OnInit {
  loading = signal(false);
  errorMessage = signal<string | null>(null);
  entries = signal<FieldSalesEntryItem[]>([]);
  totalCount = signal(0);
  reps = signal<OrgUser[]>([]);
  selectedRepId = '';

  readonly isAdminView = computed(() => {
    const role = this.auth.currentUser()?.role;
    return role === 'Owner' || role === 'Admin';
  });

  readonly totalValue = computed(() => this.entries().reduce((sum, e) => sum + e.totalCost, 0));

  // Admin-only "Sales by rep" chart -- org-wide, independent of the entries
  // table's rep filter above (a rep-comparison chart always needs every rep).
  salesByRepMetric = signal<SalesByRepMetric>('value');
  salesByRepRange = signal<ReportRange>('week');
  salesByRepCustomFrom = '';
  salesByRepCustomTo = '';
  salesByRepCustomRangeError = signal<string | null>(null);
  salesByRepRows = signal<SalesByRepRow[]>([]);
  salesByRepLoading = signal(false);
  salesByRepError = signal<string | null>(null);

  readonly salesByRepPoints = computed<ChartPoint[]>(() =>
    this.salesByRepRows().map((r) => ({
      label: r.ownerName,
      value: this.salesByRepMetric() === 'count' ? r.entryCount : r.totalValue,
    })),
  );

  constructor(
    protected readonly auth: Auth,
    private readonly api: CrmApi,
    private readonly identityApi: IdentityApi,
  ) {}

  ngOnInit(): void {
    if (this.isAdminView()) {
      this.identityApi.getOrganizationUsers().subscribe({
        next: (users) => this.reps.set(users),
        error: () => {
          // Non-fatal: the rep filter just stays empty.
        },
      });
      this.loadSalesByRep();
    }
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    const request = this.isAdminView()
      ? this.api.getSalesEntries(this.selectedRepId || undefined, 1, 100)
      : this.api.getMySalesEntries(1, 100);

    request.subscribe({
      next: (result) => {
        this.entries.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load sales entries.');
        this.loading.set(false);
      },
    });
  }

  mapUrl(entry: FieldSalesEntryItem): string | null {
    if (entry.latitude == null || entry.longitude == null) {
      return null;
    }
    return `https://www.google.com/maps?q=${entry.latitude},${entry.longitude}`;
  }

  setSalesByRepMetric(metric: SalesByRepMetric): void {
    this.salesByRepMetric.set(metric);
  }

  setSalesByRepRange(range: ReportRange): void {
    this.salesByRepRange.set(range);
    this.salesByRepCustomRangeError.set(null);
    // 'custom' just reveals the from/to inputs -- reloading waits for
    // applySalesByRepCustomRange(), same as Reports' own custom range.
    if (range !== 'custom') {
      this.loadSalesByRep();
    }
  }

  applySalesByRepCustomRange(): void {
    if (!this.salesByRepCustomFrom || !this.salesByRepCustomTo) {
      this.salesByRepCustomRangeError.set('Pick both a start and end date.');
      return;
    }
    if (this.salesByRepCustomFrom > this.salesByRepCustomTo) {
      this.salesByRepCustomRangeError.set('Start date must be on or before the end date.');
      return;
    }
    this.salesByRepCustomRangeError.set(null);
    this.loadSalesByRep();
  }

  private currentSalesByRepCustomRange(): CustomRange | undefined {
    return this.salesByRepRange() === 'custom' && this.salesByRepCustomFrom && this.salesByRepCustomTo
      ? { from: this.salesByRepCustomFrom, to: this.salesByRepCustomTo }
      : undefined;
  }

  private loadSalesByRep(): void {
    this.salesByRepLoading.set(true);
    this.salesByRepError.set(null);
    this.api.getSalesByRep(this.salesByRepRange(), this.currentSalesByRepCustomRange()).subscribe({
      next: (result) => {
        this.salesByRepRows.set(result.items);
        this.salesByRepLoading.set(false);
      },
      error: () => {
        this.salesByRepError.set('Could not load the sales-by-rep chart.');
        this.salesByRepLoading.set(false);
      },
    });
  }
}
