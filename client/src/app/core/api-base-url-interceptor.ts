import { HttpInterceptorFn } from '@angular/common/http';
import { environment } from '../../environments/environment';

/**
 * Prefixes this app's own relative `/api/...` calls with `environment.apiBaseUrl`.
 * A no-op in production, where apiBaseUrl is empty and the app is served from the
 * same origin as the API. Only under `ng serve` (different origin) does this rewrite
 * anything -- calls to other services (e.g. knightage-identity) already use an
 * absolute URL from AppConfig and are untouched.
 */
export const apiBaseUrlInterceptor: HttpInterceptorFn = (req, next) => {
  if (!environment.apiBaseUrl || !req.url.startsWith('/api')) {
    return next(req);
  }
  return next(req.clone({ url: `${environment.apiBaseUrl}${req.url}` }));
};
