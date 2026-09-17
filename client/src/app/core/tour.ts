import { Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { driver, type Driver } from 'driver.js';
import { catchError, forkJoin, map, of } from 'rxjs';
import { PagedLeads } from './models';
import { Auth } from './auth';
import { CrmApi } from './crm-api';
import { IdentityApi } from './identity-api';
import { buildTourSteps, TourStepDef } from './tour-steps';

const DESKTOP_NAV_QUERY = '(min-width: 992px)';

function resolveSelector(step: TourStepDef): string {
  if (!step.navKey) {
    return step.element;
  }
  const isDesktop = window.matchMedia(DESKTOP_NAV_QUERY).matches;
  return isDesktop ? `.sidebar [data-tour="${step.navKey}"]` : `.bottom-nav [data-tour="${step.navKey}"]`;
}

/** Polls for a selector to appear in the DOM -- covers the gap between a route navigation completing and the lazy-loaded page chunk actually rendering the target element. */
function waitForSelector(selector: string, timeoutMs: number): Promise<void> {
  return new Promise((resolve) => {
    if (document.querySelector(selector)) {
      resolve();
      return;
    }
    const observer = new MutationObserver(() => {
      if (document.querySelector(selector)) {
        observer.disconnect();
        clearTimeout(timer);
        resolve();
      }
    });
    observer.observe(document.body, { childList: true, subtree: true });
    const timer = setTimeout(() => {
      observer.disconnect();
      resolve(); // fall through -- driver.js's own skipMissingElement keeps a missing target from breaking the tour
    }, timeoutMs);
  });
}

/**
 * Drives an interactive walkthrough of the CRM app with driver.js. Follows
 * the Theme/AppConfig conventions: providedIn root, signal-based state, no
 * NgModule.
 */
@Injectable({ providedIn: 'root' })
export class Tour {
  readonly isActive = signal(false);

  private driverObj: Driver | null = null;
  private steps: TourStepDef[] = [];
  private markOnFinish = false;

  constructor(
    private readonly router: Router,
    private readonly auth: Auth,
    private readonly identityApi: IdentityApi,
    private readonly crmApi: CrmApi,
  ) {}

  /** Called from App's effect on isAuthenticated() flipping true -- auto-starts the tour exactly once per user. */
  checkAndAutoStart(): void {
    if (this.isActive()) {
      return;
    }
    this.identityApi.getMe().subscribe({
      next: (me) => {
        if (!me.hasSeenTour) {
          this.startFullTour(true);
        }
      },
      error: () => {
        // Non-fatal: no tour rather than a broken login.
      },
    });
  }

  /** Called from the topbar "Replay tour" button -- always runs, never touches the persisted flag. */
  replay(): void {
    this.startFullTour(false);
  }

  private startFullTour(markOnFinish: boolean): void {
    const role = this.auth.currentUser()?.role;
    // Fetches the Field Force Tracking flag fresh here rather than trusting OrgFeatures'
    // cached signal -- right after login that signal's own refresh() (triggered by
    // app.ts's isAuthenticated effect) is still in flight, so a synchronous read would
    // race it, same reasoning as sales-feature-guard.ts.
    forkJoin({
      leads: this.crmApi.getLeads(undefined, undefined, undefined, undefined, 1, 1).pipe(catchError(() => of({ items: [], totalCount: 0 } as PagedLeads))),
      fieldForceTrackingEnabled: this.crmApi.getOrganization().pipe(
        map((org) => org.fieldForceTrackingEnabled),
        catchError(() => of(false)),
      ),
    }).subscribe(({ leads, fieldForceTrackingEnabled }) => {
      this.launch(role, leads.items[0]?.id ?? null, fieldForceTrackingEnabled, markOnFinish);
    });
  }

  private launch(role: string | undefined, firstLeadId: string | null, fieldForceTrackingEnabled: boolean, markOnFinish: boolean): void {
    this.steps = buildTourSteps({ role, firstLeadId, fieldForceTrackingEnabled });
    if (!this.steps.length) {
      return;
    }
    this.markOnFinish = markOnFinish;
    this.isActive.set(true);

    const goTo = async (targetIndex: number): Promise<void> => {
      if (targetIndex < 0 || targetIndex >= this.steps.length) {
        this.driverObj?.destroy();
        return;
      }
      const step = this.steps[targetIndex];
      const selector = resolveSelector(step);
      if (step.route && this.router.url.split('?')[0] !== step.route) {
        await this.router.navigateByUrl(step.route);
        await waitForSelector(selector, 3000);
      } else {
        // Yield even when no navigation is needed -- calling driverObj.drive()
        // synchronously from inside its own onNextClick/onPrevClick handler
        // (same call stack as the button click) re-enters driver.js mid-update
        // and leaves the popover stuck at display:none. A tick's delay lets
        // driver.js finish handling the click before we drive it again.
        await new Promise((resolve) => setTimeout(resolve, 0));
      }
      this.driverObj?.drive(targetIndex);
    };

    this.driverObj = driver({
      showProgress: true,
      overlayOpacity: 0.5,
      steps: this.steps.map((s, i) => ({
        element: resolveSelector(s),
        popover: {
          title: s.title,
          description: s.description,
          side: s.side,
          onNextClick: () => {
            void goTo(i + 1);
          },
          onPrevClick: () => {
            void goTo(i - 1);
          },
        },
      })),
      onDestroyed: () => this.finish(),
    });

    this.router.navigateByUrl('/dashboard').then(() => goTo(0));
  }

  private finish(): void {
    this.isActive.set(false);
    this.driverObj = null;
    if (this.markOnFinish) {
      this.markOnFinish = false;
      this.identityApi.markTourCompleted().subscribe({
        error: () => {
          // Non-fatal: worst case the tour auto-starts again next login.
        },
      });
    }
  }
}
