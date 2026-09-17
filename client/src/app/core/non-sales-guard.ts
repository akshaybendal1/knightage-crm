import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { Auth } from './auth';

// Blocks a Sales-role user from reaching the regular Dashboard/Leads/Pipeline
// Stages/Tasks/Calendar/Timeline/Contact Us routes by typing the URL directly --
// app.html already hides the nav links to these, this is the matching route-level
// enforcement (same defense-in-depth relationship admin-guard.ts has to its own
// [Authorize(Roles=...)] server-side check).
export const nonSalesGuard: CanActivateFn = () => {
  const auth = inject(Auth);
  if (auth.currentUser()?.role !== 'Sales') {
    return true;
  }
  inject(Router).navigateByUrl('/sales-entry');
  return false;
};
