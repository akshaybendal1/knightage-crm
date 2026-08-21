import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { Auth } from './auth';

// Defense in depth for the /team route -- the server already rejects non-admins
// with 403, this just avoids flashing the page before that response comes back.
export const adminGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  const role = auth.currentUser()?.role;
  if (role === 'Owner' || role === 'Admin') {
    return true;
  }
  inject(Router).navigateByUrl('/dashboard');
  return false;
};
