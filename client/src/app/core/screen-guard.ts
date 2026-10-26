import { CanActivateFn, Router } from '@angular/router';
import { inject } from '@angular/core';
import { catchError, map, of } from 'rxjs';
import { ScreenAccess } from './screen-access';

/**
 * Lets a route open only if the signed-in user's role has the screen (Access Control settings, or the
 * built-in defaults). Anyone else is sent to the first screen they may open, so a Sales user typing /leads
 * lands on their own home, exactly as before. The server separately refuses the data behind the screens
 * where it checks roles; this is what keeps the page itself from opening.
 */
export const screenGuard = (screenKey: string): CanActivateFn => () => {
  const access = inject(ScreenAccess);
  const router = inject(Router);
  return access.ensureLoaded().pipe(
    map((allowed) => (allowed.has(screenKey) ? true : router.createUrlTree([access.homeUrl()]))),
    // If the list can't be loaded, don't open a screen on a guess: My Profile needs no permission.
    catchError(() => of(router.createUrlTree(['/profile']))),
  );
};
