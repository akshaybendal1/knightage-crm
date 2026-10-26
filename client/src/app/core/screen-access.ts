import { Injectable, computed, signal } from '@angular/core';
import { Observable, finalize, map, of, shareReplay, tap } from 'rxjs';
import { CrmApi } from './crm-api';

// Where to send someone who opens a screen their role can't, or who has just signed in:
// the first screen they may open, in this order. My Profile is the always-available fallback.
const HOME_PRIORITY: { key: string; url: string }[] = [
  { key: 'dashboard', url: '/dashboard' },
  { key: 'sales-entry', url: '/sales-entry' },
  { key: 'sales-dashboard', url: '/sales-dashboard' },
  { key: 'leads', url: '/leads' },
  { key: 'customers', url: '/customers' },
  { key: 'tasks', url: '/tasks' },
  { key: 'timeline', url: '/timeline' },
  { key: 'calendar', url: '/calendar' },
  { key: 'pipeline-stages', url: '/pipeline-stages' },
  { key: 'reports', url: '/reports' },
  { key: 'products', url: '/products' },
  { key: 'product-categories', url: '/product-categories' },
  { key: 'message-templates', url: '/message-templates' },
  { key: 'sales-coverage', url: '/sales-coverage' },
  { key: 'team', url: '/team' },
  { key: 'organization', url: '/organization' },
  { key: 'access-control', url: '/access-control' },
  { key: 'contact-us', url: '/contact-us' },
];

/**
 * Which screens the signed-in user's role may open, as set on the Access Control screen (or the
 * built-in defaults). Drives the sidebar and the route guards. The list comes from
 * GET /api/screen-access/me; nothing is assumed before it arrives, so the menu simply waits for
 * `loaded()` instead of flashing the wrong items.
 */
@Injectable({ providedIn: 'root' })
export class ScreenAccess {
  private readonly allowed = signal<ReadonlySet<string> | null>(null);
  readonly loaded = computed(() => this.allowed() !== null);
  private inflight: Observable<ReadonlySet<string>> | null = null;

  constructor(private readonly api: CrmApi) {}

  can(key: string): boolean {
    return this.allowed()?.has(key) ?? false;
  }

  canAny(keys: readonly string[]): boolean {
    return keys.some((k) => this.can(k));
  }

  /** The first screen this user may open -- used for "/" after sign-in and when a screen is refused. */
  homeUrl(): string {
    const set = this.allowed();
    return HOME_PRIORITY.find((h) => set?.has(h.key))?.url ?? '/profile';
  }

  /**
   * Resolves with the allowed screens, fetching them first if that hasn't happened yet. Route guards use
   * this rather than reading the signal, because right after sign-in the shell's own refresh is still in
   * flight and a synchronous read would see "nothing allowed" and bounce a valid user.
   */
  ensureLoaded(): Observable<ReadonlySet<string>> {
    const current = this.allowed();
    return current ? of(current) : this.load();
  }

  /** Re-reads from the server -- on sign-in, and after the Access Control screen saves a change. */
  refresh(): void {
    this.inflight = null;
    this.load().subscribe({ error: () => { /* keep whatever was known; guards handle a failed first load */ } });
  }

  clear(): void {
    this.allowed.set(null);
    this.inflight = null;
  }

  private load(): Observable<ReadonlySet<string>> {
    if (!this.inflight) {
      this.inflight = this.api.getMyScreens().pipe(
        map((result) => new Set(result.screens) as ReadonlySet<string>),
        tap((set) => this.allowed.set(set)),
        finalize(() => (this.inflight = null)),
        shareReplay(1),
      );
    }
    return this.inflight;
  }
}
