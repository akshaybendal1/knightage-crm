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
    this.api.createPipelineStage({ name: this.name, sortOrder: this.sortOrder }).subscribe({
      next: () => {
        this.name = '';
        this.sortOrder = this.stages().length + 1;
        this.load();
      },
      error: () => this.errorMessage.set('Could not create pipeline stage.'),
    });
  }
}
