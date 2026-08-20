import { Component, effect, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Auth } from './core/auth';
import { CrmApi } from './core/crm-api';
import { isDueToday, isOverdue } from './core/task-status';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  dueTaskCount = signal(0);

  constructor(
    protected readonly auth: Auth,
    private readonly api: CrmApi,
  ) {
    // Reruns whenever isAuthenticated() flips true -- covers both an existing
    // session on page load and a fresh login within the same app instance.
    effect(() => {
      if (this.auth.isAuthenticated()) {
        this.loadDueTaskCount();
      }
    });
  }

  private loadDueTaskCount(): void {
    this.api.getMyTasks('Open').subscribe({
      next: (tasks) => this.dueTaskCount.set(tasks.filter((t) => isOverdue(t) || isDueToday(t)).length),
      error: () => {
        // Non-fatal: the reminder badge just stays at its last known count.
      },
    });
  }
}
