// The sibling Knightage apps listed under "Apps" in the sidebar. Same file in knightage-crm,
// knightage-accounting and knightage-inventory-sales -- keep the copies in step.
//
// Links go to each app's /auth-callback?token=, the same handoff knightage-platform's service
// launcher uses, so the user lands already signed in. Production hosts are fixed; anywhere else
// (local dev) points at each API's launchSettings port, which also serves the built SPA.

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

export function otherKnightageApps(current: KnightageAppKey): KnightageApp[] {
  return APPS.filter((app) => app.key !== current);
}

export function knightageAppUrl(key: KnightageAppKey, token: string | null): string {
  const isProduction = window.location.hostname.endsWith('knightagetech.com');
  const base = (isProduction ? PRODUCTION_URLS : LOCAL_URLS)[key];
  return token ? `${base}/auth-callback?token=${encodeURIComponent(token)}` : base;
}
