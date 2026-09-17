import { Injectable, signal } from '@angular/core';

export type ThemeName = 'light' | 'dark';

const STORAGE_KEY = 'knightage_theme';

/**
 * Parses a #RRGGBB hex string into an rgba(...) string at the given alpha --
 * mirrors how --accent-soft is already a 10%-opacity version of --accent in
 * styles.css's default (rgba(230, 98, 57, .1)). Returns null for anything
 * that isn't a valid 6-digit hex color, so callers can fall back gracefully.
 */
function hexToRgba(hex: string, alpha: number): string | null {
  const match = /^#([0-9a-fA-F]{6})$/.exec(hex);
  if (!match) {
    return null;
  }
  const value = match[1];
  const r = parseInt(value.slice(0, 2), 16);
  const g = parseInt(value.slice(2, 4), 16);
  const b = parseInt(value.slice(4, 6), 16);
  return `rgba(${r}, ${g}, ${b}, ${alpha})`;
}

/**
 * Defaults to dark for anyone without a stored preference -- that's the
 * theme this app shipped with, so a first-time visitor sees what they saw
 * before this toggle existed. A returning visitor's explicit choice always
 * wins over that default.
 */
const DEFAULT_THEME: ThemeName = 'dark';

@Injectable({
  providedIn: 'root',
})
export class Theme {
  readonly current = signal<ThemeName>(this.loadStored() ?? DEFAULT_THEME);

  constructor() {
    this.apply(this.current());
  }

  toggle(): void {
    this.set(this.current() === 'dark' ? 'light' : 'dark');
  }

  set(theme: ThemeName): void {
    this.current.set(theme);
    localStorage.setItem(STORAGE_KEY, theme);
    this.apply(theme);
  }

  private apply(theme: ThemeName): void {
    document.documentElement.setAttribute('data-theme', theme);
  }

  /**
   * Applies a tenant's platform-managed accent color, or reverts to the
   * app's default when there isn't one (or it's not stored yet). This is
   * server-driven per-org branding, not a user preference -- unlike the
   * light/dark toggle above, it's never written to localStorage.
   */
  applyAccentColor(color: string | null | undefined): void {
    const root = document.documentElement.style;
    const soft = color ? hexToRgba(color, 0.1) : null;
    if (color && soft) {
      root.setProperty('--accent', color);
      root.setProperty('--accent-soft', soft);
    } else {
      root.removeProperty('--accent');
      root.removeProperty('--accent-soft');
    }
  }

  private loadStored(): ThemeName | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw === 'light' || raw === 'dark' ? raw : null;
  }
}
