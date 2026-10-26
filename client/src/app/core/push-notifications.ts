import { Injectable } from '@angular/core';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { Capacitor } from '@capacitor/core';
// Aliased -- the plugin's own exported class is also named PushNotifications, which
// would otherwise collide with this file's own exported class name.
import { PushNotifications as Fcm } from '@capacitor/push-notifications';
import { CrmApi } from './crm-api';
import { Toast } from './toast';

@Injectable({ providedIn: 'root' })
export class PushNotifications {
  private registeredToken: string | null = null;

  constructor(
    private readonly api: CrmApi,
    private readonly router: Router,
    private readonly toast: Toast,
  ) {}

  async init(): Promise<void> {
    // Same isNative gate login.ts uses for the native Google Sign-In button -- push
    // registration only makes sense inside the Capacitor Android app, never on web.
    if (!Capacitor.isNativePlatform() || Capacitor.getPlatform() !== 'android') {
      return;
    }

    try {
      const permission = await Fcm.requestPermissions();
      if (permission.receive !== 'granted') {
        return;
      }
      await Fcm.register();
    } catch {
      // No push rather than a broken page -- everything else about the app works fine
      // without it.
      return;
    }

    Fcm.addListener('registration', (token) => {
      this.registeredToken = token.value;
      this.api.registerDeviceToken(token.value, 'android').subscribe({
        error: () => {
          // Best-effort: a failed registration just means no push this session: the
          // next app open (and the next 'registration' event) tries again.
        },
      });
    });

    Fcm.addListener('registrationError', () => {
      // No push rather than a broken page.
    });

    // Android does NOT auto-show a system-tray notification while the app is in the
    // foreground, even though the FCM message carries a `notification` block -- only
    // this listener fires. Without it, a reminder that lands while the CRM happens to
    // be open would be silently swallowed.
    Fcm.addListener('pushNotificationReceived', (notification) => {
      this.toast.success(`${notification.title ?? 'Reminder'}: ${notification.body ?? ''}`);
    });

    Fcm.addListener('pushNotificationActionPerformed', (action) => {
      const data = action.notification.data as { leadId?: string; type?: string };
      if (data?.leadId) {
        this.router.navigate(['/leads', data.leadId], { queryParams: { open: data.type === 'meeting' ? 'meeting' : 'task' } });
      } else {
        // The digest spanned more than one lead -- no single correct place to land,
        // so go to the Tasks list instead of guessing.
        this.router.navigateByUrl('/tasks');
      }
    });
  }

  async handleLogout(): Promise<void> {
    const token = this.registeredToken;
    if (!token) {
      return;
    }
    this.registeredToken = null;
    try {
      await firstValueFrom(this.api.removeDeviceToken(token));
    } catch {
      // Best-effort -- logout must never be blocked by this.
    }
  }
}
