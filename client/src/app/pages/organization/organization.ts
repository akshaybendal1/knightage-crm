import { Component, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppConfig } from '../../core/app-config';
import { Auth } from '../../core/auth';
import { Branding } from '../../core/branding';
import { CrmApi } from '../../core/crm-api';
import { SubscriptionStatus } from '../../core/models';
import { Toast } from '../../core/toast';
import { Loader } from '../../loader/loader';

const MAX_LOGO_BYTES = 2 * 1024 * 1024;
const ALLOWED_LOGO_TYPES = ['image/png', 'image/jpeg'];

@Component({
  selector: 'app-organization',
  imports: [FormsModule, DatePipe, Loader],
  templateUrl: './organization.html',
  styleUrl: './organization.css',
})
export class Organization implements OnInit {
  loading = signal(false);
  saving = signal(false);
  requestingSubscription = signal(false);
  requestSent = signal(false);
  errorMessage = signal<string | null>(null);
  subscriptionStatus = signal<SubscriptionStatus | null>(null);
  subscriptionStartDate = signal<string | null>(null);
  subscriptionEndDate = signal<string | null>(null);

  name = '';
  contactEmail = '';
  contactPhone = '';
  address = '';

  selectedLogoFile: File | null = null;
  brandingError = signal<string | null>(null);
  uploadingLogo = signal(false);
  removingLogo = signal(false);

  constructor(
    private readonly api: CrmApi,
    private readonly toast: Toast,
    protected readonly branding: Branding,
    protected readonly appConfig: AppConfig,
    private readonly auth: Auth,
  ) {}

  // Same handoff platform's own service launcher uses (services.ts's open()), reversed:
  // pass this session's JWT via ?token= so platform's /auth-callback logs the user straight
  // into /services instead of its login form -- both apps trust tokens from the same
  // identity issuer, so no separate sign-in is needed.
  platformUrl(): string {
    const token = this.auth.token;
    if (!token) {
      return this.appConfig.platformBaseUrl;
    }
    return `${this.appConfig.platformBaseUrl}/auth-callback?token=${encodeURIComponent(token)}`;
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.loading.set(true);
    this.errorMessage.set(null);
    this.api.getOrganization().subscribe({
      next: (org) => {
        this.name = org.name;
        this.contactEmail = org.contactEmail ?? '';
        this.contactPhone = org.contactPhone ?? '';
        this.address = org.address ?? '';
        this.subscriptionStatus.set(org.subscriptionStatus);
        this.subscriptionStartDate.set(org.subscriptionStartDate ?? null);
        this.subscriptionEndDate.set(org.subscriptionEndDate ?? null);
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load your organization.');
        this.loading.set(false);
      },
    });
  }

  save(): void {
    this.saving.set(true);
    this.errorMessage.set(null);
    this.api
      .updateOrganization({
        name: this.name.trim(),
        contactEmail: this.contactEmail.trim() || undefined,
        contactPhone: this.contactPhone.trim() || undefined,
        address: this.address.trim() || undefined,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.toast.success('Organization details saved.');
        },
        error: (err) => {
          this.saving.set(false);
          this.toast.error(err?.error?.message ?? 'Could not save organization details.');
        },
      });
  }

  requestSubscription(): void {
    this.requestingSubscription.set(true);
    this.api.requestSubscription().subscribe({
      next: () => {
        this.requestingSubscription.set(false);
        this.requestSent.set(true);
        this.toast.success('Request sent.');
      },
      error: (err) => {
        this.requestingSubscription.set(false);
        this.toast.error(err?.error?.message ?? 'Could not send the request.');
      },
    });
  }

  statusLabel(): string {
    switch (this.subscriptionStatus()) {
      case 'Active':
        return 'Active';
      case 'ReadOnly':
        return 'Grace Period';
      case 'Disabled':
        return 'Expired';
      case 'NotSubscribed':
        return 'Not Subscribed';
      default:
        return '';
    }
  }

  statusBadgeClass(): string {
    switch (this.subscriptionStatus()) {
      case 'Active':
        return 'badge-success';
      case 'ReadOnly':
        return 'badge-warning';
      case 'Disabled':
        return 'badge-danger';
      case 'NotSubscribed':
        return 'badge-secondary';
      default:
        return 'badge-secondary';
    }
  }

  onLogoFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0] ?? null;
    this.brandingError.set(null);

    if (!file) {
      this.selectedLogoFile = null;
      return;
    }
    if (!ALLOWED_LOGO_TYPES.includes(file.type)) {
      this.brandingError.set('Logo must be a PNG or JPEG image.');
      this.selectedLogoFile = null;
      input.value = '';
      return;
    }
    if (file.size > MAX_LOGO_BYTES) {
      this.brandingError.set('Logo must be 2 MB or smaller.');
      this.selectedLogoFile = null;
      input.value = '';
      return;
    }
    this.selectedLogoFile = file;
  }

  uploadLogo(): void {
    if (!this.selectedLogoFile) {
      return;
    }
    this.brandingError.set(null);
    this.uploadingLogo.set(true);
    this.api.uploadBrandingLogo(this.selectedLogoFile).subscribe({
      next: () => {
        this.uploadingLogo.set(false);
        this.selectedLogoFile = null;
        this.branding.refresh();
        this.toast.success('Logo updated.');
      },
      error: (err) => {
        this.uploadingLogo.set(false);
        this.brandingError.set(err?.error?.message ?? 'Could not upload the logo.');
      },
    });
  }

  removeLogo(): void {
    if (!confirm('Remove your organization logo? Invoices and quotations will go back to text-only.')) {
      return;
    }
    this.removingLogo.set(true);
    this.api.deleteBrandingLogo().subscribe({
      next: () => {
        this.removingLogo.set(false);
        this.branding.refresh();
        this.toast.success('Logo removed.');
      },
      error: () => {
        this.removingLogo.set(false);
        this.toast.error('Could not remove the logo.');
      },
    });
  }
}
