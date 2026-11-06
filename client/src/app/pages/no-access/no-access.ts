import { Component, computed, inject } from '@angular/core';
import { Auth } from '../../core/auth';
import { ScreenAccess } from '../../core/screen-access';
import { APP_SERVICE, KnightageAppKey, knightageAppUrl, otherKnightageApps, showAppSwitchScreen } from '../../core/knightage-apps';

/** Shown instead of CRM to someone whose administrator has turned CRM off for them (Platform -> Users). */
@Component({
  selector: 'app-no-access',
  templateUrl: './no-access.html',
})
export class NoAccess {
  protected readonly auth = inject(Auth);
  private readonly access = inject(ScreenAccess);

  protected readonly apps = computed(() =>
    otherKnightageApps('crm').filter((app) => {
      const service = APP_SERVICE[app.key];
      return service === null || this.access.canOpenApp(service);
    }),
  );

  open(event: MouseEvent, key: KnightageAppKey): void {
    event.preventDefault();
    showAppSwitchScreen(key);
    window.location.href = knightageAppUrl(key, this.auth.token);
  }

  url(key: KnightageAppKey): string {
    return knightageAppUrl(key, null);
  }
}
