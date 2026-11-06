import { Injectable, Injector, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { tap } from 'rxjs';
import { AppConfig } from './app-config';
import { Auth } from './auth';
import { SharedRequest } from './shared-request';
import { CalendarConnection, CurrentUser, OrgUser, TeamMember } from './models';

@Injectable({
  providedIn: 'root',
})
export class IdentityApi {
  constructor(
    private readonly http: HttpClient,
    private readonly appConfig: AppConfig,
  ) {}

  getOrganizationUsers() {
    return this.http.get<OrgUser[]>(`${this.appConfig.identityBaseUrl}/api/users`);
  }

  getTeam() {
    return this.http.get<TeamMember[]>(`${this.appConfig.identityBaseUrl}/api/team`);
  }

  addTeamMember(payload: { displayName: string; email: string; password: string; role: 'Admin' | 'Member' | 'Sales'; phoneNumber?: string }) {
    return this.http.post<TeamMember>(`${this.appConfig.identityBaseUrl}/api/team`, payload);
  }

  updateTeamMemberRole(userId: string, role: 'Admin' | 'Member' | 'Sales') {
    return this.http.post<void>(`${this.appConfig.identityBaseUrl}/api/team/${userId}/role`, { role });
  }

  setTeamMemberActive(userId: string, isActive: boolean) {
    return this.http.post<void>(`${this.appConfig.identityBaseUrl}/api/team/${userId}/status`, { isActive });
  }

  // The tour and the dashboard reminders both load the signed-in user when a page opens: one shared request (SharedRequest).
  private readonly injector = inject(Injector);
  private readonly me = new SharedRequest(() => this.http.get<CurrentUser>(`${this.appConfig.identityBaseUrl}/api/users/me`), 10_000);

  getMe() {
    return this.me.get(this.injector.get(Auth).token);
  }

  markTourCompleted() {
    return this.http.post<void>(`${this.appConfig.identityBaseUrl}/api/users/me/tour-completed`, {}).pipe(tap(() => this.me.clear()));
  }

  updateProfile(displayName: string, phoneNumber: string | null) {
    return this.http
      .post<CurrentUser>(`${this.appConfig.identityBaseUrl}/api/users/me`, { displayName, phoneNumber })
      .pipe(tap(() => this.me.clear()));
  }

  changePassword(currentPassword: string, newPassword: string) {
    return this.http.post<void>(`${this.appConfig.identityBaseUrl}/api/users/me/change-password`, { currentPassword, newPassword });
  }

  deleteMyAccount(confirmEmail: string, password: string | null) {
    return this.http.post<void>(`${this.appConfig.identityBaseUrl}/api/users/me/delete`, { confirmEmail, password });
  }

  getCalendarConnection() {
    return this.http.get<CalendarConnection>(`${this.appConfig.identityBaseUrl}/api/calendar/connection`);
  }

  connectCalendar(code: string) {
    return this.http.post<CalendarConnection>(`${this.appConfig.identityBaseUrl}/api/calendar/connection`, { code });
  }

  disconnectCalendar() {
    return this.http.post<void>(`${this.appConfig.identityBaseUrl}/api/calendar/connection/disconnect`, {});
  }
}
