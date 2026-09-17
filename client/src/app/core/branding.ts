import { Injectable, signal } from '@angular/core';
import { CrmApi } from './crm-api';

/**
 * Applies a tenant's uploaded logo to the sidebar and browser-tab favicon.
 * Server-driven per-org branding, not a user preference -- mirrors Theme's
 * applyAccentColor in shape, but the logo itself has to be fetched via an
 * authenticated call and turned into an object URL (a plain <img>/<link>
 * can't send the Authorization header a per-tenant image needs).
 */
@Injectable({ providedIn: 'root' })
export class Branding {
  readonly logoUrl = signal<string | null>(null);

  private readonly defaultFaviconHref: string | null;
  private objectUrl: string | null = null;

  constructor(private readonly api: CrmApi) {
    this.defaultFaviconHref = this.faviconLink()?.href ?? null;
  }

  refresh(): void {
    this.api.getBrandingLogo().subscribe((blob) => {
      if (this.objectUrl) {
        URL.revokeObjectURL(this.objectUrl);
        this.objectUrl = null;
      }

      if (!blob) {
        this.logoUrl.set(null);
        this.applyFavicon(this.defaultFaviconHref);
        return;
      }

      this.objectUrl = URL.createObjectURL(blob);
      this.logoUrl.set(this.objectUrl);
      this.applyFavicon(this.objectUrl);
    });
  }

  private faviconLink(): HTMLLinkElement | null {
    return document.querySelector('link[rel="icon"]');
  }

  private applyFavicon(href: string | null): void {
    const link = this.faviconLink();
    if (link && href) {
      link.href = href;
    }
  }
}
