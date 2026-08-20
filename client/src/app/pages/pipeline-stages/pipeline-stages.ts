import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CrmApi } from '../../core/crm-api';
import { PipelineStage } from '../../core/models';

@Component({
  selector: 'app-pipeline-stages',
  imports: [FormsModule],
  templateUrl: './pipeline-stages.html',
  styleUrl: './pipeline-stages.css',
})
export class PipelineStages implements OnInit {
  stages = signal<PipelineStage[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);

  name = '';
  sortOrder = 1;
  isWon = false;
  isLost = false;

  editingId = signal<string | null>(null);
  editName = '';
  editSortOrder = 1;
  editIsWon = false;
  editIsLost = false;

  constructor(private readonly api: CrmApi) {}

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.api.getPipelineStages().subscribe({
      next: (stages) => {
        this.stages.set(stages);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load pipeline stages.');
        this.loading.set(false);
      },
    });
  }

  createStage(): void {
    this.errorMessage.set(null);
    this.api.createPipelineStage({ name: this.name, sortOrder: this.sortOrder, isWon: this.isWon, isLost: this.isLost }).subscribe({
      next: () => {
        this.name = '';
        this.sortOrder = this.stages().length + 1;
        this.isWon = false;
        this.isLost = false;
        this.load();
      },
      error: () => this.errorMessage.set('Could not create pipeline stage.'),
    });
  }

  startEdit(stage: PipelineStage): void {
    this.editingId.set(stage.id);
    this.editName = stage.name;
    this.editSortOrder = stage.sortOrder;
    this.editIsWon = stage.isWon;
    this.editIsLost = stage.isLost;
  }

  cancelEdit(): void {
    this.editingId.set(null);
  }

  saveEdit(id: string): void {
    this.api
      .updatePipelineStage(id, {
        name: this.editName,
        sortOrder: this.editSortOrder,
        isWon: this.editIsWon,
        isLost: this.editIsLost,
      })
      .subscribe({
        next: () => {
          this.editingId.set(null);
          this.load();
        },
        error: () => this.errorMessage.set('Could not save changes.'),
      });
  }
}
