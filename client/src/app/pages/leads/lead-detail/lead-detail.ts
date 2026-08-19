import { Component, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { CrmApi } from '../../../core/crm-api';
import { Lead, LeadActivity, PipelineStage } from '../../../core/models';

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
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  newNote = '';
  savingNote = signal(false);

  private leadId = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly api: CrmApi,
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
    this.api.getLeadActivities(this.leadId).subscribe({
      next: (activities) => this.activities.set(activities),
      error: () => this.errorMessage.set('Could not load the activity timeline.'),
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
