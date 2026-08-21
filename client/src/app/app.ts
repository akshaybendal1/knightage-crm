import { Component, HostListener, effect, signal } from '@angular/core';
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
  sidebarOpen = signal(false);
  userMenuOpen = signal(false);

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

    // KaiAdmin's CSS keys the mobile slide-out sidebar off body.nav_open --
    // there's no JS bundled for it here (see app.css comment), so this drives
    // the same class by hand instead of shipping jQuery/Bootstrap JS for one toggle.
    effect(() => {
      document.body.classList.toggle('nav_open', this.sidebarOpen());
    });
  }

  initials(): string {
    const name = this.auth.currentUser()?.displayName ?? '';
    return name
      .split(/\s+/)
      .filter(Boolean)
      .slice(0, 2)
      .map((part) => part[0]!.toUpperCase())
      .join('');
  }

  toggleSidebar(): void {
    this.sidebarOpen.update((open) => !open);
  }

  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }

  toggleUserMenu(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.userMenuOpen.update((open) => !open);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    if (this.userMenuOpen() && !target.closest('.topbar-user')) {
      this.userMenuOpen.set(false);
    }
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
