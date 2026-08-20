import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AppConfig } from './app-config';
import { OrgUser } from './models';

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
}
