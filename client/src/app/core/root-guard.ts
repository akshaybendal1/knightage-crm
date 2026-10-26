import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { Auth } from './auth';

// Only the bare root path ("/") uses this -- everywhere else, an unauthenticated
// visit still goes through authGuard straight to /login as before (a stale
// bookmark or an expired session should land you back at sign-in, not a
// marketing page). The login page itself links to /about ("Know more about
// product") for a cold visitor who wants context first.
export const rootGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  inject(Router).navigateByUrl(auth.isAuthenticated() ? '/dashboard' : '/login');
  return false;
};
