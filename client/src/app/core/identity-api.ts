import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AppConfig } from './app-config';
import { OrgUser, TeamMember } from './models';

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

  addTeamMember(payload: { displayName: string; email: string; password: string; role: 'Admin' | 'Member' }) {
    return this.http.post<TeamMember>(`${this.appConfig.identityBaseUrl}/api/team`, payload);
  }

  updateTeamMemberRole(userId: string, role: 'Admin' | 'Member') {
    return this.http.put<void>(`${this.appConfig.identityBaseUrl}/api/team/${userId}/role`, { role });
  }

  setTeamMemberActive(userId: string, isActive: boolean) {
    return this.http.put<void>(`${this.appConfig.identityBaseUrl}/api/team/${userId}/status`, { isActive });
  }
}
