import { loadGoogleIdentityScript } from './google-identity';

declare const google: any;

const CALENDAR_SCOPE = 'https://www.googleapis.com/auth/calendar.events';

/**
 * Wraps GIS's code-client popup flow to obtain a one-time authorization code for
 * Calendar access, which the caller POSTs to knightage-identity to exchange server-side.
 *
 * Two load-bearing parameters: access_type: 'offline' is what makes Google issue a
 * refresh token at all -- without it there is nothing to store server-side, and the
 * whole feature has no point. prompt: 'consent' forces the consent screen on every
 * call, since Google only issues a refresh token on a user's *first* consent for a
 * given client+scope; skipping this would make a reconnect after a disconnect silently
 * come back with no refresh token at all.
 *
 * Reuses loadGoogleIdentityScript() since google.accounts.oauth2 ships in the same
 * already-loaded GIS script as google.accounts.id (the existing Sign-In flow).
 */
export function requestCalendarConnection(clientId: string): Promise<string> {
  return loadGoogleIdentityScript().then(
    () =>
      new Promise<string>((resolve, reject) => {
        const client = google.accounts.oauth2.initCodeClient({
          client_id: clientId,
          scope: CALENDAR_SCOPE,
          access_type: 'offline',
          prompt: 'consent',
          ux_mode: 'popup',
          callback: (response: any) => {
            if (response?.code) {
              resolve(response.code);
            } else {
              reject(new Error(response?.error ?? 'Google Calendar connection was cancelled or failed.'));
            }
          },
        });
        client.requestCode();
      }),
  );
}
