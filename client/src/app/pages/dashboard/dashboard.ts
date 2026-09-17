import { Component, OnInit, computed, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { CrmApi } from '../../core/crm-api';
import { PipelineSummaryItem, WonLostSummary } from '../../core/models';
import { Loader } from '../../loader/loader';
import { UpcomingReminders } from './upcoming-reminders/upcoming-reminders';

type WonLostRange = 'week' | 'month' | 'all';
type PipelineMode = 'count' | 'value';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink, CurrencyPipe, Loader, UpcomingReminders],
  templateUrl: './dashboard.html',
})
export class Dashboard implements OnInit {
  pipelineMode = signal<PipelineMode>('count');
  pipelineSummary = signal<PipelineSummaryItem[]>([]);
  wonLost = signal<WonLostSummary | null>(null);
  activityCount = signal<number | null>(null);
  overdueTaskCount = signal<number | null>(null);
  stuckLeadsCount = signal<number | null>(null);
  range = signal<WonLostRange>('week');
  errorMessage = signal<string | null>(null);

  // Five independent requests fire on load -- track each so the page-wide
  // loading gate clears only once all of them have settled (success or error),
  // instead of racing to whichever happens to finish first.
  private pipelineLoaded = signal(false);
  private wonLostLoaded = signal(false);
  private activityLoaded = signal(false);
  private overdueLoaded = signal(false);
  private stuckLoaded = signal(false);
  loading = computed(
    () => !(this.pipelineLoaded() && this.wonLostLoaded() && this.activityLoaded() && this.overdueLoaded() && this.stuckLoaded()),
  );

  // Separate from the page-wide gate: switching the Won/Lost range re-fires just
  // that one request, and shouldn't hide the other three cards while it's in flight.
  wonLostLoading = signal(false);

  constructor(private readonly api: CrmApi) {}

  ngOnInit(): void {
    this.api.getPipelineSummary().subscribe({
      next: (summary) => {
        this.pipelineSummary.set(summary);
        this.pipelineLoaded.set(true);
      },
      error: () => {
        this.errorMessage.set('Could not load the pipeline summary.');
        this.pipelineLoaded.set(true);
      },
    });
    this.loadWonLost();
    this.api.getActivitySummary().subscribe({
      next: (result) => {
        this.activityCount.set(result.count);
        this.activityLoaded.set(true);
      },
      error: () => {
        this.errorMessage.set('Could not load the activity summary.');
        this.activityLoaded.set(true);
      },
    });
    this.api.getOverdueTaskCount().subscribe({
      next: (result) => {
        this.overdueTaskCount.set(result.count);
        this.overdueLoaded.set(true);
      },
      error: () => {
        this.errorMessage.set('Could not load overdue task count.');
        this.overdueLoaded.set(true);
      },
    });
    this.api.getStuckLeadsCount().subscribe({
      next: (result) => {
        this.stuckLeadsCount.set(result.count);
        this.stuckLoaded.set(true);
      },
      error: () => {
        this.errorMessage.set('Could not load stuck leads count.');
        this.stuckLoaded.set(true);
      },
    });
  }

  setPipelineMode(mode: PipelineMode): void {
    this.pipelineMode.set(mode);
  }

  setRange(range: WonLostRange): void {
    this.range.set(range);
    this.loadWonLost();
  }

  private loadWonLost(): void {
    this.wonLostLoading.set(true);
    this.api.getWonLostSummary(this.range()).subscribe({
      next: (summary) => {
        this.wonLost.set(summary);
        this.wonLostLoaded.set(true);
        this.wonLostLoading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load the won/lost summary.');
        this.wonLostLoaded.set(true);
        this.wonLostLoading.set(false);
      },
    });
  }
}
