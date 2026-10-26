import { DOCUMENT } from '@angular/common';
import { Injectable, inject } from '@angular/core';
import { Meta } from '@angular/platform-browser';

const SITE_ORIGIN = 'https://crm.knightagetech.com';

// Route.title (set directly on each Route in app.routes.ts) already updates
// document.title automatically via Angular Router's built-in TitleStrategy --
// this only covers what Angular has no built-in per-route mechanism for: the
// meta description and the canonical link. Both only matter for the handful of
// public routes robots.txt actually allows (see app.routes.ts's `description`
// route data); every other route is intentionally left alone, since it's never
// meant to be indexed in the first place.
@Injectable({ providedIn: 'root' })
export class Seo {
  private readonly meta = inject(Meta);
  private readonly document = inject(DOCUMENT);

  update(description: string | undefined, path: string): void {
    if (description) {
      this.meta.updateTag({ name: 'description', content: description });
      this.meta.updateTag({ property: 'og:description', content: description });
      this.meta.updateTag({ name: 'twitter:description', content: description });
    }

    const href = `${SITE_ORIGIN}${path}`;
    let link = this.document.querySelector<HTMLLinkElement>("link[rel='canonical']");
    if (!link) {
      link = this.document.createElement('link');
      link.setAttribute('rel', 'canonical');
      this.document.head.appendChild(link);
    }
    link.setAttribute('href', href);
  }
}
