import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { Auth } from './auth';

// Only the bare root path ("/") uses this -- everywhere else, an unauthenticated
// visit still goes through authGuard straight to /login as before (a stale
// bookmark or an expired session should land you back at sign-in, not a
// marketing page). The login page itself links to /about ("Know more about
// product") for a cold visitor who wants context first.
//
// Returned as a redirect rather than started with navigateByUrl(): a returned redirect inherits the original
// navigation's replaceUrl, so /auth-callback's hand-off (which replaces itself) doesn't leave "/" or the
// token URL behind for Back to bounce on -- the Android app's Back button relies on that.
export const rootGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  return inject(Router).parseUrl(auth.isAuthenticated() ? '/dashboard' : '/login');
};
