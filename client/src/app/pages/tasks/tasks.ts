import { Component, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { CrmApi } from '../../core/crm-api';
import { LeadTask } from '../../core/models';
import { isOverdue } from '../../core/task-status';

@Component({
  selector: 'app-tasks',
  imports: [RouterLink, DatePipe],
  templateUrl: './tasks.html',
  styleUrl: './tasks.css',
})
export class Tasks implements OnInit {
  tasks = signal<LeadTask[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);
  showingAll = false;

  readonly isOverdue = isOverdue;

  constructor(
    private readonly api: CrmApi,
    private readonly route: ActivatedRoute,
  ) {}

  ngOnInit(): void {
    this.showingAll = this.route.snapshot.queryParamMap.get('scope') === 'all';
    this.loadTasks();
  }

  loadTasks(): void {
    this.loading.set(true);
    this.api.getMyTasks('Open', this.showingAll).subscribe({
      next: (tasks) => {
        this.tasks.set(tasks);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load your tasks.');
        this.loading.set(false);
      },
    });
  }

  completeTask(task: LeadTask): void {
    this.api.updateTaskStatus(task.id, 'Completed').subscribe({
      next: () => this.loadTasks(),
      error: () => this.errorMessage.set('Could not update this task.'),
    });
  }
}
