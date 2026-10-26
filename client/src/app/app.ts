import { Component, HostListener, computed, effect, signal } from '@angular/core';
import { ActivatedRoute, NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { Auth } from './core/auth';
import { Branding } from './core/branding';
import { CrmApi } from './core/crm-api';
import { KnightageAppKey, knightageAppUrl, otherKnightageApps } from './core/knightage-apps';
import { OrgFeatures } from './core/org-features';
import { ScreenAccess } from './core/screen-access';
import { PlanUsage, SubscriptionStatus } from './core/models';
import { PushNotifications } from './core/push-notifications';
import { Seo } from './core/seo';
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
  // See isSignInPath below.
  private readonly onSignInPage = signal(isSignInPath(window.location.pathname));
  protected readonly showShell = computed(() => this.auth.isAuthenticated() && !this.onSignInPage());
  dueTaskCount = signal(0);
  subscriptionStatus = signal<SubscriptionStatus | null>(null);
  organizationName = signal<string | null>(null);
  recordUsage = signal<PlanUsage | null>(null);
  userUsage = signal<PlanUsage | null>(null);
  // Hard limit is enforced server-side (403/409) regardless of this banner -- this is
  // purely the "you're close" / "you're at it" heads-up, at 90% of either cap.
  recordUsageWarning = computed(() => usageWarning(this.recordUsage(), 'records'));
  userUsageWarning = computed(() => usageWarning(this.userUsage(), 'users'));
  sidebarOpen = signal(false);

  // Screens that mean "this user works in the full CRM". Someone whose role has none of them (the
  // default Sales role) gets the compact field-sales menu instead of the grouped one.
  private static readonly FULL_CRM_SCREENS = [
    'dashboard', 'leads', 'pipeline-stages', 'timeline', 'tasks', 'calendar',
    'products', 'product-categories', 'reports', 'message-templates', 'team', 'organization', 'access-control',
  ];
  protected readonly mainScreens = ['dashboard', 'leads', 'customers', 'tasks', 'calendar'];
  protected readonly pipelineScreens = ['pipeline-stages', 'timeline'];
  protected readonly catalogScreens = ['products', 'product-categories'];
  protected readonly settingsScreens = ['team', 'message-templates', 'organization', 'access-control'];
  protected readonly fieldSalesScreens = ['sales-entry', 'sales-dashboard', 'sales-coverage'];
  protected readonly compactMenu = computed(() => this.screenAccess.loaded() && !this.screenAccess.canAny(App.FULL_CRM_SCREENS));
  // Persisted separately from sidebarOpen: that signal is the ephemeral mobile
  // off-canvas state (always starts closed), while this is the desktop
  // hide/show preference, which should survive a reload once someone sets it.
  desktopSidebarVisible = signal<boolean>(localStorage.getItem('crm-sidebar-visible') !== 'false');
  userMenuOpen = signal(false);
  readonly currentYear = new Date().getFullYear();
  readonly otherApps = otherKnightageApps('crm');

  // Which collapsible sidebar submenus (Pipeline/Catalog/Insights/Settings/Field
  // Sales) are expanded -- the group containing the current route starts open so
  // navigating straight to e.g. /organization doesn't hide where you landed.
  private static readonly NAV_GROUP_ROUTES: Record<string, string[]> = {
    pipeline: ['/pipeline-stages', '/timeline'],
    catalog: ['/products', '/product-categories'],
    insights: ['/reports'],
    settings: ['/team', '/organization', '/message-templates', '/access-control'],
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
    protected readonly screenAccess: ScreenAccess,
    private readonly pushNotifications: PushNotifications,
    private readonly router: Router,
    private readonly activatedRoute: ActivatedRoute,
    private readonly seo: Seo,
  ) {
    this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe((event) => {
      this.onSignInPage.set(isSignInPath((event as NavigationEnd).urlAfterRedirects));
    });
    // Reruns whenever isAuthenticated() flips true -- covers both an existing
    // session on page load and a fresh login within the same app instance. Also the
    // natural re-registration point if a different user logs into a shared device.
    effect(() => {
      if (this.auth.isAuthenticated()) {
        this.loadDueTaskCount();
        this.loadOrganizationStatus();
        this.tour.checkAndAutoStart();
        this.branding.refresh();
        this.orgFeatures.refresh();
        this.screenAccess.refresh();
        this.pushNotifications.init();
      } else {
        // A different user may sign in next on this page; never carry the last user's menu over.
        this.screenAccess.clear();
      }
    });

    // Route.title (on each Route in app.routes.ts) already updates document.title
    // automatically via the router's own TitleStrategy -- this only handles what
    // has no built-in per-route mechanism: meta description and the canonical
    // link, read from the leaf route's `data.description` (see core/seo.ts).
    this.router.events.pipe(filter((event) => event instanceof NavigationEnd)).subscribe((event) => {
      let route = this.activatedRoute;
      while (route.firstChild) {
        route = route.firstChild;
      }
      this.seo.update(route.snapshot.data['description'], (event as NavigationEnd).urlAfterRedirects);
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

  // The href is the bare site (for middle-click / copy link); a normal click carries this
  // session's token so the other app opens signed in.
  appHomeUrl(key: KnightageAppKey): string {
    return knightageAppUrl(key, null);
  }

  openApp(event: MouseEvent, key: KnightageAppKey): void {
    if (event.button !== 0 || event.ctrlKey || event.metaKey || event.shiftKey) {
      return;
    }
    event.preventDefault();
    window.location.href = knightageAppUrl(key, this.auth.token);
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
        this.recordUsage.set(org.recordUsage);
        this.userUsage.set(org.userUsage);
      },
      error: () => {
        // Non-fatal: no banner shown rather than a broken one.
      },
    });
  }
}

function usageWarning(usage: PlanUsage | null, label: string): string | null {
  if (!usage || usage.max <= 0) {
    return null;
  }
  if (usage.current >= usage.max) {
    return `This organization is at its plan's limit of ${usage.max} ${label}. Ask your admin to upgrade the plan.`;
  }
  if (usage.current / usage.max >= 0.9) {
    return `This organization is close to its plan's ${label} limit (${usage.current} of ${usage.max}).`;
  }
  return null;
}

// The sign-in pages store the session a moment before they navigate away. Showing the shell as soon as
// the user is authenticated would draw it around the still-visible login form for that moment (an
// "after-image" of the login page), so the shell waits until the router has actually left these pages.
function isSignInPath(url: string): boolean {
  const path = url.split(/[?#]/)[0];
  return path === '/login' || path === '/auth-callback';
}
