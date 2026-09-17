import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { map, catchError, of } from 'rxjs';
import { Auth } from './auth';
import { CrmApi } from './crm-api';
import { Toast } from './toast';

// Guards /sales-entry and /sales-dashboard: both need the org's Field Force Tracking
// flag on, checked client-side here purely to avoid flashing the page before the
// server's own check (SalesEntriesController) would 403 it anyway.
//
// Fetches GET /api/organization directly rather than reading OrgFeatures' cached
// signal -- right after login, that signal's own refresh() (triggered by app.ts's
// isAuthenticated effect) is still an in-flight async call, so a synchronous read
// here would race it and see the pre-load default (false), incorrectly bouncing a
// freshly-logged-in Sales user straight back out. A direct fetch has no such gap.
export const salesFeatureGuard: CanActivateFn = () => {
  const api = inject(CrmApi);
  const auth = inject(Auth);
  const router = inject(Router);
  const toast = inject(Toast);

  return api.getOrganization().pipe(
    map((org) => {
      if (org.fieldForceTrackingEnabled) {
        return true;
      }
      if (auth.currentUser()?.role === 'Sales') {
        // A Sales user has no other usable page in this app -- if their org's admin
        // hasn't (or hasn't yet -- server-side flag changes can take up to 15 minutes
        // to take effect, see PlatformSubscriptionStatusProvider's cache) turned this
        // feature on, log them out rather than leaving them on a route with nowhere
        // left to go. Show *why* first -- a silent logout here is indistinguishable
        // from a broken login from the user's side.
        toast.error("Field Force Tracking isn't enabled for your organization yet. Contact your administrator.");
        auth.logout();
      } else {
        router.navigateByUrl('/dashboard');
      }
      return false;
    }),
    catchError(() => {
      router.navigateByUrl('/dashboard');
      return of(false);
    }),
  );
};
