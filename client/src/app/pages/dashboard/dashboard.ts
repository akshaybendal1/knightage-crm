import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CrmApi } from '../../core/crm-api';
import { PipelineSummaryItem, WonLostSummary } from '../../core/models';

type WonLostRange = 'week' | 'month' | 'all';

@Component({
  selector: 'app-dashboard',
  imports: [RouterLink],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css',
})
export class Dashboard implements OnInit {
  pipelineSummary = signal<PipelineSummaryItem[]>([]);
  wonLost = signal<WonLostSummary | null>(null);
  activityCount = signal<number | null>(null);
  overdueTaskCount = signal<number | null>(null);
  range = signal<WonLostRange>('week');
  errorMessage = signal<string | null>(null);

  constructor(private readonly api: CrmApi) {}

  ngOnInit(): void {
    this.api.getPipelineSummary().subscribe({
      next: (summary) => this.pipelineSummary.set(summary),
      error: () => this.errorMessage.set('Could not load the pipeline summary.'),
    });
    this.loadWonLost();
    this.api.getActivitySummary().subscribe({
      next: (result) => this.activityCount.set(result.count),
      error: () => this.errorMessage.set('Could not load the activity summary.'),
    });
    this.api.getOverdueTaskCount().subscribe({
      next: (result) => this.overdueTaskCount.set(result.count),
      error: () => this.errorMessage.set('Could not load overdue task count.'),
    });
  }

  maxLeadCount(): number {
    return Math.max(1, ...this.pipelineSummary().map((s) => s.leadCount));
  }

  barWidth(count: number): number {
    return Math.round((count / this.maxLeadCount()) * 100);
  }

  setRange(range: WonLostRange): void {
    this.range.set(range);
    this.loadWonLost();
  }

  private loadWonLost(): void {
    this.api.getWonLostSummary(this.range()).subscribe({
      next: (summary) => this.wonLost.set(summary),
      error: () => this.errorMessage.set('Could not load the won/lost summary.'),
    });
  }
}
