import { Injectable, signal } from '@angular/core';
import { CrmApi } from './crm-api';

/**
 * Whether this org's Field Force Tracking module (Add Sales Entry / Sales Dashboard)
 * is turned on -- set from knightage-platform's Tenants page, surfaced through
 * GET /api/organization alongside subscriptionStatus/accentColor (see
 * PlatformSubscriptionStatusProvider). A standalone signal service, mirroring
 * Branding, so route guards (which can't reach into the App component directly) can
 * inject it too.
 */
@Injectable({ providedIn: 'root' })
export class OrgFeatures {
  readonly fieldForceTrackingEnabled = signal(false);

  constructor(private readonly api: CrmApi) {}

  refresh(): void {
    this.api.getOrganization().subscribe({
      next: (org) => this.fieldForceTrackingEnabled.set(org.fieldForceTrackingEnabled),
      error: () => {
        // Non-fatal: keep the last known value rather than flipping features off
        // because of a transient load failure.
      },
    });
  }
}
