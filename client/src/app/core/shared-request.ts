import { Observable, catchError, shareReplay, throwError } from 'rxjs';

/**
 * Lets everything that asks for the same thing at about the same time share one HTTP request. When a page opens, the
 * shell, the feature flags, route guards, the tour and the dashboard each load the organization or the current user --
 * two or three identical requests per page load, each a full round trip (about 0.3 s on production).
 *
 * The answer is reused for `reuseMs` within the same sign-in (`key`, the token, so another account never sees it), then
 * fetched fresh; clear() drops it after a change. A failed request is not kept, so the next caller tries again.
 */
export class SharedRequest<T> {
  private cached: { key: string | null; at: number; value$: Observable<T> } | null = null;

  constructor(
    private readonly load: () => Observable<T>,
    private readonly reuseMs: number,
  ) {}

  get(key: string | null): Observable<T> {
    const cached = this.cached;
    if (cached && cached.key === key && Date.now() - cached.at < this.reuseMs) {
      return cached.value$;
    }

    const value$: Observable<T> = this.load().pipe(
      catchError((error) => {
        if (this.cached?.value$ === value$) {
          this.cached = null;
        }
        return throwError(() => error);
      }),
      shareReplay({ bufferSize: 1, refCount: false }),
    );
    this.cached = { key, at: Date.now(), value$ };
    return value$;
  }

  clear(): void {
    this.cached = null;
  }
}
