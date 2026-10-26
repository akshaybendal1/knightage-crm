import { Component, OnInit, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { Auth } from '../../core/auth';
import { IdentityApi } from '../../core/identity-api';
import { Toast } from '../../core/toast';
import { Loader } from '../../loader/loader';

@Component({
  selector: 'app-profile',
  imports: [FormsModule, RouterLink, Loader],
  templateUrl: './profile.html',
})
export class Profile implements OnInit {
  loading = signal(true);
  saving = signal(false);
  errorMessage = signal<string | null>(null);
  email = '';
  role = '';

  displayName = '';
  phoneNumber = '';

  changingPassword = signal(false);
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  passwordError = signal<string | null>(null);

  hasPassword = false;
  deleting = signal(false);
  deleteConfirmEmail = '';
  deletePassword = '';
  deleteError = signal<string | null>(null);

  constructor(
    private readonly identityApi: IdentityApi,
    protected readonly auth: Auth,
    private readonly toast: Toast,
  ) {}

  ngOnInit(): void {
    this.identityApi.getMe().subscribe({
      next: (user) => {
        this.displayName = user.displayName;
        this.phoneNumber = user.phoneNumber ?? '';
        this.email = user.email;
        this.role = user.role;
        this.hasPassword = user.hasPassword ?? true;
        this.loading.set(false);
      },
      error: () => {
        this.errorMessage.set('Could not load your profile.');
        this.loading.set(false);
      },
    });
  }

  saveProfile(): void {
    if (!this.displayName.trim()) {
      this.errorMessage.set('Name is required.');
      return;
    }
    this.saving.set(true);
    this.errorMessage.set(null);
    this.identityApi.updateProfile(this.displayName.trim(), this.phoneNumber.trim() || null).subscribe({
      next: () => {
        this.saving.set(false);
        this.auth.updateCachedDisplayName(this.displayName.trim());
        this.toast.success('Profile updated.');
      },
      error: (err) => {
        this.saving.set(false);
        this.errorMessage.set(err?.error?.message ?? 'Could not update your profile.');
      },
    });
  }

  canDeleteAccount(): boolean {
    return (
      this.deleteConfirmEmail.trim().toLowerCase() === this.email.toLowerCase() &&
      (!this.hasPassword || this.deletePassword.length > 0)
    );
  }

  deleteAccount(): void {
    if (!this.canDeleteAccount()) {
      return;
    }
    this.deleting.set(true);
    this.deleteError.set(null);
    this.identityApi.deleteMyAccount(this.deleteConfirmEmail.trim(), this.hasPassword ? this.deletePassword : null).subscribe({
      next: () => {
        this.deleting.set(false);
        this.toast.success('Your account has been deleted.');
        this.auth.logout();
      },
      error: (err) => {
        this.deleting.set(false);
        this.deleteError.set(err?.error?.message ?? 'Could not delete your account.');
      },
    });
  }

  changePassword(): void {
    this.passwordError.set(null);
    if (this.newPassword.length < 8) {
      this.passwordError.set('New password must be at least 8 characters.');
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.passwordError.set('New password and confirmation do not match.');
      return;
    }
    this.changingPassword.set(true);
    this.identityApi.changePassword(this.currentPassword, this.newPassword).subscribe({
      next: () => {
        this.changingPassword.set(false);
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
        this.toast.success('Password changed.');
      },
      error: (err) => {
        this.changingPassword.set(false);
        this.passwordError.set(err?.error?.message ?? 'Could not change your password.');
      },
    });
  }
}
