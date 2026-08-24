import { Component, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CdkDragDrop, DragDropModule, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { CrmApi } from '../../../core/crm-api';
import { Lead, PipelineStage } from '../../../core/models';

interface Column {
  stage: PipelineStage;
  leads: Lead[];
}

@Component({
  selector: 'app-kanban',
  imports: [DragDropModule, RouterLink],
  templateUrl: './kanban.html',
  styleUrl: './kanban.css',
})
export class Kanban implements OnInit {
  columns = signal<Column[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);
  movingLeadId = signal<string | null>(null);

  constructor(private readonly api: CrmApi) {}

  ngOnInit(): void {
    this.load();
  }

  connectedListIds(): string[] {
    return this.columns().map((c) => c.stage.id);
  }

  load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.api.getPipelineStages().subscribe({
      next: (stages) => {
        const activeStages = stages.filter((s) => s.isActive).sort((a, b) => a.sortOrder - b.sortOrder);
        this.api.getLeads().subscribe({
          next: (leads) => {
            this.columns.set(
              activeStages.map((stage) => ({
                stage,
                leads: leads.filter((l) => l.pipelineStageId === stage.id),
              })),
            );
            this.loading.set(false);
          },
          error: () => {
            this.errorMessage.set('Could not load leads.');
            this.loading.set(false);
          },
        });
      },
      error: () => {
        this.errorMessage.set('Could not load pipeline stages.');
        this.loading.set(false);
      },
    });
  }

  drop(event: CdkDragDrop<Lead[]>, targetStageId: string): void {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
      return;
    }

    const lead = event.previousContainer.data[event.previousIndex];
    const previousStageId = lead.pipelineStageId;
    transferArrayItem(event.previousContainer.data, event.container.data, event.previousIndex, event.currentIndex);
    lead.pipelineStageId = targetStageId;

    this.movingLeadId.set(lead.id);
    this.api
      .updateLead(lead.id, {
        name: lead.name,
        email: lead.email ?? undefined,
        phone: lead.phone ?? undefined,
        company: lead.company ?? undefined,
        pipelineStageId: targetStageId,
        notes: lead.notes ?? undefined,
      })
      .subscribe({
        next: () => this.movingLeadId.set(null),
        error: () => {
          this.movingLeadId.set(null);
          lead.pipelineStageId = previousStageId;
          this.errorMessage.set(`Could not move "${lead.name}" to that stage.`);
          this.load();
        },
      });
  }
}
