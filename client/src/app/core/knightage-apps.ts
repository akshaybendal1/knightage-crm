// The sibling Knightage apps listed under "Apps" in the sidebar. Same file in knightage-crm,
// knightage-accounting and knightage-inventory-sales -- keep the copies in step.
//
// A click opens the other app through Auth.handoffUrl(): its /auth-callback with a one-time sign-in code, the same
// hand-over knightage-platform's service launcher uses, so the user lands already signed in. Production hosts are
// fixed; anywhere else (local dev) points at each API's launchSettings port, which also serves the built SPA.

export type KnightageAppKey = 'platform' | 'crm' | 'accounting' | 'inventory';

export interface KnightageApp {
  key: KnightageAppKey;
  name: string;
  icon: string;
}

const APPS: KnightageApp[] = [
  { key: 'platform', name: 'Platform', icon: 'fas fa-th-large' },
  { key: 'crm', name: 'CRM', icon: 'fas fa-handshake' },
  { key: 'accounting', name: 'Accounting', icon: 'fas fa-calculator' },
  { key: 'inventory', name: 'Inventory', icon: 'fas fa-boxes' },
];

const PRODUCTION_URLS: Record<KnightageAppKey, string> = {
  platform: 'https://platform.knightagetech.com',
  crm: 'https://crm.knightagetech.com',
  accounting: 'https://account.knightagetech.com',
  inventory: 'https://ias.knightagetech.com',
};

const LOCAL_URLS: Record<KnightageAppKey, string> = {
  platform: 'http://localhost:5102',
  crm: 'http://localhost:5105',
  accounting: 'http://localhost:5104',
  inventory: 'http://localhost:5106',
};

/** The Platform service key behind each app, for "may this user open it?" (Platform -> Users). Platform itself: always. */
export const APP_SERVICE: Record<KnightageAppKey, 'Crm' | 'InventorySales' | 'Accounting' | null> = {
  platform: null,
  crm: 'Crm',
  accounting: 'Accounting',
  inventory: 'InventorySales',
};

export function otherKnightageApps(current: KnightageAppKey): KnightageApp[] {
  return APPS.filter((app) => app.key !== current);
}

/** The app's own address (its home, or sign-in page when signed out there). */
export function knightageAppUrl(key: KnightageAppKey): string {
  const isProduction = window.location.hostname.endsWith('knightagetech.com');
  return (isProduction ? PRODUCTION_URLS : LOCAL_URLS)[key];
}

// Switching apps is a full page load, so the old page would otherwise sit there unchanged until the other app's first
// paint. Cover it straight away with the screen that app shows while it loads (index.html's .boot-splash, same name and
// accent), so the tap answers at once and the switch reads as one step. Removed again if the browser restores this page
// from its back/forward cache (Back from the other app).
const SWITCH_SCREENS: Record<KnightageAppKey, { mark: string; text: string; accent: string }> = {
  platform: { mark: '', text: 'Opening your services…', accent: '#4f6df5' },
  crm: { mark: 'CRM', text: 'Opening CRM…', accent: '#e66239' },
  accounting: { mark: 'Accounting', text: 'Opening Accounting…', accent: '#e66239' },
  inventory: { mark: 'Inventory', text: 'Opening Inventory…', accent: '#e66239' },
};

export function showAppSwitchScreen(key: KnightageAppKey): void {
  const screen = SWITCH_SCREENS[key];
  const el = document.createElement('div');
  el.className = 'boot-splash';
  el.setAttribute('role', 'status');
  el.setAttribute('aria-live', 'polite');
  el.style.setProperty('--boot-accent', screen.accent);
  el.innerHTML =
    `<div class="boot-splash-mark">Knightage<span>${screen.mark}</span></div>` +
    '<div class="boot-splash-spinner" aria-hidden="true"></div>' +
    `<div class="boot-splash-text">${screen.text}</div>`;
  document.body.appendChild(el);
  window.addEventListener('pageshow', (event) => event.persisted && el.remove());
}
