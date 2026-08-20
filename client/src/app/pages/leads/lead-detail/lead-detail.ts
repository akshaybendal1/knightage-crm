import { Component, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { CrmApi } from '../../../core/crm-api';
import { Auth } from '../../../core/auth';
import { Lead, LeadActivity, PipelineStage } from '../../../core/models';
import { timeAgo } from '../../../core/time-ago';

const ACTIVITIES_PAGE_SIZE = 20;

@Component({
  selector: 'app-lead-detail',
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './lead-detail.html',
  styleUrl: './lead-detail.css',
})
export class LeadDetail implements OnInit {
  lead = signal<Lead | null>(null);
  stages = signal<PipelineStage[]>([]);
  activities = signal<LeadActivity[]>([]);
  hasMoreActivities = signal(false);
  loadingMoreActivities = signal(false);
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  newNote = '';
  savingNote = signal(false);

  readonly timeAgo = timeAgo;

  private leadId = '';
  private activitiesPage = 1;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly api: CrmApi,
    private readonly auth: Auth,
  ) {}

  ngOnInit(): void {
    this.leadId = this.route.snapshot.paramMap.get('id') ?? '';
    this.api.getPipelineStages().subscribe((stages) => this.stages.set(stages));
    this.loadLead();
    this.loadActivities();
  }

  stageName(id: string): string {
    return this.stages().find((s) => s.id === id)?.name ?? id;
  }

  authorLabel(activity: LeadActivity): string {
    if (!activity.createdByUserId) {
      return 'System';
    }
    if (activity.createdByUserId === this.auth.currentUser()?.userId) {
      return 'You';
    }
    return `User ${activity.createdByUserId.slice(0, 8)}`;
  }

  activityTypeClass(type: string): string {
    switch (type) {
      case 'StageChange':
        return 'badge-stage-change';
      case 'LeadCreated':
        return 'badge-lead-created';
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
        this.loadActivities();
      },
      error: () => {
        this.savingNote.set(false);
        this.errorMessage.set('Could not save this note.');
      },
    });
  }
}
