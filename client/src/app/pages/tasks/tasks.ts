import { Component, OnInit, computed, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { CrmApi } from '../../core/crm-api';
import { IdentityApi } from '../../core/identity-api';
import { Auth } from '../../core/auth';
import { LeadTask, OrgUser } from '../../core/models';
import { isOverdue } from '../../core/task-status';
import { Toast } from '../../core/toast';
import { Loader } from '../../loader/loader';

@Component({
  selector: 'app-tasks',
  imports: [RouterLink, DatePipe, Loader],
  templateUrl: './tasks.html',
  styleUrl: './tasks.css',
})
export class Tasks implements OnInit {
  tasks = signal<LeadTask[]>([]);
  orgUsers = signal<OrgUser[]>([]);
  loading = signal(false);
  errorMessage = signal<string | null>(null);
  showingAll = false;

  readonly pageSize = 20;
  page = signal(1);
  totalCount = signal(0);
  totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / this.pageSize)));
  pageStart = computed(() => (this.totalCount() === 0 ? 0 : (this.page() - 1) * this.pageSize + 1));
  pageEnd = computed(() => Math.min(this.page() * this.pageSize, this.totalCount()));

  readonly isOverdue = isOverdue;

  constructor(
    private readonly api: CrmApi,
    private readonly identityApi: IdentityApi,
    private readonly auth: Auth,
    private readonly route: ActivatedRoute,
    private readonly toast: Toast,
  ) {}

  ngOnInit(): void {
    this.showingAll = this.route.snapshot.queryParamMap.get('scope') === 'all';
    this.identityApi.getOrganizationUsers().subscribe({
      next: (users) => this.orgUsers.set(users),
      error: () => {
        // Non-fatal: attribution just falls back to raw user ids.
      },
    });
    this.loadTasks();
  }

  userLabel(userId: string | null | undefined): string {
    if (!userId) {
      return 'System';
    }
    if (userId === this.auth.currentUser()?.userId) {
      return 'You';
    }
    return this.orgUsers().find((u) => u.id === userId)?.displayName ?? `User ${userId.slice(0, 8)}`;
  }

  loadTasks(): void {
    this.loading.set(true);
    this.api.getMyTasks('Open', this.showingAll, this.page(), this.pageSize).subscribe({
      next: (result) => {
        this.tasks.set(result.items);
        this.totalCount.set(result.totalCount);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load your tasks.');
        this.loading.set(false);
      },
    });
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page()) {
      return;
    }
    this.page.set(page);
    this.loadTasks();
  }

  completeTask(task: LeadTask): void {
    this.api.updateTaskStatus(task.id, 'Completed').subscribe({
      next: () => {
        this.toast.success('Task completed.');
        this.loadTasks();
      },
      error: () => this.toast.error('Could not update this task.'),
    });
  }
}
