// Pure helpers for the Sales Dashboard's admin-only coverage view -- no Angular
// dependencies, so these can be unit tested (or reasoned about) in isolation from
// the component/map wiring.

export type CoverageGranularity = 'day' | 'week' | 'month';

export interface PeriodRange {
  from: Date;
  to: Date;
  label: string;
}

const MONTH_NAMES = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
const SHORT_MONTH_NAMES = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];

function startOfDay(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), date.getDate());
}

// Monday-based week, matching this app's existing server-side week boundary
// (DashboardController.StartOfWeekUtc).
function startOfWeek(date: Date): Date {
  const d = startOfDay(date);
  const daysSinceMonday = (d.getDay() + 6) % 7; // Sunday=0 -> 6, Monday=1 -> 0, ...
  d.setDate(d.getDate() - daysSinceMonday);
  return d;
}

function startOfMonth(date: Date): Date {
  return new Date(date.getFullYear(), date.getMonth(), 1);
}

function formatShort(date: Date): string {
  return `${SHORT_MONTH_NAMES[date.getMonth()]} ${date.getDate()}, ${date.getFullYear()}`;
}

/** Calendar-boundary [from, to] (inclusive) for the period containing `anchor`, plus a display label. */
export function getPeriodRange(granularity: CoverageGranularity, anchor: Date): PeriodRange {
  if (granularity === 'day') {
    const from = startOfDay(anchor);
    return { from, to: from, label: formatShort(from) };
  }

  if (granularity === 'week') {
    const from = startOfWeek(anchor);
    const to = new Date(from);
    to.setDate(to.getDate() + 6);
    const sameMonth = from.getMonth() === to.getMonth() && from.getFullYear() === to.getFullYear();
    const label = sameMonth
      ? `${SHORT_MONTH_NAMES[from.getMonth()]} ${from.getDate()} – ${to.getDate()}, ${to.getFullYear()}`
      : `${formatShort(from)} – ${formatShort(to)}`;
    return { from, to, label };
  }

  const from = startOfMonth(anchor);
  const to = new Date(from.getFullYear(), from.getMonth() + 1, 0);
  return { from, to, label: `${MONTH_NAMES[from.getMonth()]} ${from.getFullYear()}` };
}

/** The new anchor date after moving one period forward/back. */
export function shiftPeriod(granularity: CoverageGranularity, anchor: Date, direction: -1 | 1): Date {
  const d = new Date(anchor);
  if (granularity === 'day') {
    d.setDate(d.getDate() + direction);
  } else if (granularity === 'week') {
    d.setDate(d.getDate() + direction * 7);
  } else {
    d.setMonth(d.getMonth() + direction);
  }
  return d;
}

/** yyyy-MM-dd in local time, matching what the /coverage endpoint expects. */
export function toDateOnlyString(date: Date): string {
  const y = date.getFullYear();
  const m = String(date.getMonth() + 1).padStart(2, '0');
  const d = String(date.getDate()).padStart(2, '0');
  return `${y}-${m}-${d}`;
}

export interface CoverageResult {
  /** Convex hull, in the same lat/lng shape the map layer needs. Empty when fewer than 3 points. */
  hull: { lat: number; lng: number }[];
  areaKm2: number;
}

interface PlanarPoint {
  x: number;
  y: number;
  lat: number;
  lng: number;
}

const EARTH_RADIUS_M = 6371000;

/** Cross product of OA x OB, used by the convex-hull sweep below. */
function cross(o: PlanarPoint, a: PlanarPoint, b: PlanarPoint): number {
  return (a.x - o.x) * (b.y - o.y) - (a.y - o.y) * (b.x - o.x);
}

/** Andrew's monotone-chain convex hull. Returns points in counter-clockwise order. */
function convexHull(points: PlanarPoint[]): PlanarPoint[] {
  const sorted = [...points].sort((a, b) => (a.x === b.x ? a.y - b.y : a.x - b.x));
  if (sorted.length < 3) {
    return sorted;
  }

  const buildChain = (pts: PlanarPoint[]): PlanarPoint[] => {
    const chain: PlanarPoint[] = [];
    for (const p of pts) {
      while (chain.length >= 2 && cross(chain[chain.length - 2], chain[chain.length - 1], p) <= 0) {
        chain.pop();
      }
      chain.push(p);
    }
    return chain;
  };

  const lower = buildChain(sorted);
  const upper = buildChain([...sorted].reverse());
  lower.pop();
  upper.pop();
  return [...lower, ...upper];
}

/** Shoelace formula. `pts` must already be an ordered (convex) polygon. */
function polygonAreaM2(pts: PlanarPoint[]): number {
  let sum = 0;
  for (let i = 0; i < pts.length; i++) {
    const a = pts[i];
    const b = pts[(i + 1) % pts.length];
    sum += a.x * b.y - b.x * a.y;
  }
  return Math.abs(sum) / 2;
}

/**
 * Projects points to local planar meters (equirectangular approximation centered on
 * the centroid -- accurate enough at city/region scale, which is what a field rep's
 * daily/weekly/monthly coverage actually is), takes the convex hull, and returns its
 * area. Needs at least 3 distinct points for a non-degenerate polygon; fewer than
 * that returns an empty hull and zero area so the caller can show "not enough
 * locations" instead of a misleading 0.
 */
export function computeCoverage(points: { latitude: number; longitude: number }[]): CoverageResult {
  if (points.length < 3) {
    return { hull: [], areaKm2: 0 };
  }

  const lat0 = (points.reduce((sum, p) => sum + p.latitude, 0) / points.length) * (Math.PI / 180);
  const lng0 = (points.reduce((sum, p) => sum + p.longitude, 0) / points.length) * (Math.PI / 180);

  const planar: PlanarPoint[] = points.map((p) => {
    const latRad = p.latitude * (Math.PI / 180);
    const lngRad = p.longitude * (Math.PI / 180);
    return {
      x: EARTH_RADIUS_M * (lngRad - lng0) * Math.cos(lat0),
      y: EARTH_RADIUS_M * (latRad - lat0),
      lat: p.latitude,
      lng: p.longitude,
    };
  });

  const hull = convexHull(planar);
  if (hull.length < 3) {
    return { hull: [], areaKm2: 0 };
  }

  const areaKm2 = polygonAreaM2(hull) / 1_000_000;
  return { hull: hull.map((p) => ({ lat: p.lat, lng: p.lng })), areaKm2 };
}
