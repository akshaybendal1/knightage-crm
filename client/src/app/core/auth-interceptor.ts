import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { Auth } from './auth';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const token = inject(Auth).token;
  if (!token) {
    return next(req);
  }
  return next(req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })).pipe(
    catchError((error: unknown) => {
      // An administrator took CRM away from this user while they had it open (Platform -> Users): reload into
      // the "no access" page rather than leave a screen whose every call now fails.
      if (error instanceof HttpErrorResponse && error.status === 403 && error.error?.appAccessDenied && !location.pathname.startsWith('/no-access')) {
        location.assign('/no-access');
      }
      return throwError(() => error);
    }),
  );
};
