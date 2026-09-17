import { Component, HostListener, effect, signal } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Auth } from './core/auth';
import { Branding } from './core/branding';
import { CrmApi } from './core/crm-api';
import { OrgFeatures } from './core/org-features';
import { SubscriptionStatus } from './core/models';
import { Theme } from './core/theme';
import { Tour } from './core/tour';
import { ToastContainer } from './toast-container/toast-container';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastContainer],
  templateUrl: './app.html',
  styleUrl: './app.css',
})
export class App {
  dueTaskCount = signal(0);
  subscriptionStatus = signal<SubscriptionStatus | null>(null);
  organizationName = signal<string | null>(null);
  sidebarOpen = signal(false);
  // Persisted separately from sidebarOpen: that signal is the ephemeral mobile
  // off-canvas state (always starts closed), while this is the desktop
  // hide/show preference, which should survive a reload once someone sets it.
  desktopSidebarVisible = signal<boolean>(localStorage.getItem('crm-sidebar-visible') !== 'false');
  userMenuOpen = signal(false);
  readonly currentYear = new Date().getFullYear();

  // Which collapsible sidebar submenus (Pipeline/Catalog/Insights/Settings/Field
  // Sales) are expanded -- the group containing the current route starts open so
  // navigating straight to e.g. /organization doesn't hide where you landed.
  private static readonly NAV_GROUP_ROUTES: Record<string, string[]> = {
    pipeline: ['/pipeline-stages', '/timeline'],
    catalog: ['/products', '/product-categories'],
    insights: ['/reports'],
    settings: ['/team', '/organization', '/message-templates'],
    fieldSales: ['/sales-entry', '/sales-dashboard', '/sales-coverage'],
  };
  openNavGroups = signal<Set<string>>(this.initialOpenNavGroups());

  constructor(
    protected readonly auth: Auth,
    private readonly api: CrmApi,
    protected readonly theme: Theme,
    protected readonly tour: Tour,
    protected readonly branding: Branding,
    protected readonly orgFeatures: OrgFeatures,
  ) {
    // Reruns whenever isAuthenticated() flips true -- covers both an existing
    // session on page load and a fresh login within the same app instance.
    effect(() => {
      if (this.auth.isAuthenticated()) {
        this.loadDueTaskCount();
        this.loadOrganizationStatus();
        this.tour.checkAndAutoStart();
        this.branding.refresh();
        this.orgFeatures.refresh();
      }
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
    if (window.innerWidth >= 992) {
      this.desktopSidebarVisible.update((visible) => {
        const next = !visible;
        localStorage.setItem('crm-sidebar-visible', String(next));
        return next;
      });
    } else {
      this.sidebarOpen.update((open) => !open);
    }
  }

  closeSidebar(): void {
    this.sidebarOpen.set(false);
  }

  private initialOpenNavGroups(): Set<string> {
    const path = window.location.pathname;
    const open = new Set<string>();
    for (const [group, routes] of Object.entries(App.NAV_GROUP_ROUTES)) {
      if (routes.some((r) => path.startsWith(r))) {
        open.add(group);
      }
    }
    return open;
  }

  isNavGroupOpen(group: string): boolean {
    return this.openNavGroups().has(group);
  }

  toggleNavGroup(group: string): void {
    this.openNavGroups.update((groups) => {
      const next = new Set(groups);
      if (next.has(group)) {
        next.delete(group);
      } else {
        next.add(group);
      }
      return next;
    });
  }

  toggleUserMenu(event: Event): void {
    event.preventDefault();
    event.stopPropagation();
    this.userMenuOpen.update((open) => !open);
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: Event): void {
    const target = event.target as HTMLElement;
    if (this.userMenuOpen() && !target.closest('.user-menu')) {
      this.userMenuOpen.set(false);
    }
  }

  private loadDueTaskCount(): void {
    this.api.getDueTaskCount().subscribe({
      next: (result) => this.dueTaskCount.set(result.count),
      error: () => {
        // Non-fatal: the reminder badge just stays at its last known count.
      },
    });
  }

  private loadOrganizationStatus(): void {
    this.api.getOrganization().subscribe({
      next: (org) => {
        this.subscriptionStatus.set(org.subscriptionStatus);
        this.theme.applyAccentColor(org.accentColor);
        this.organizationName.set(org.name);
      },
      error: () => {
        // Non-fatal: no banner shown rather than a broken one.
      },
    });
  }
}
