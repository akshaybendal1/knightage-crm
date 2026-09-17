import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe } from '@angular/common';
import * as L from 'leaflet';
import { CrmApi } from '../../core/crm-api';
import { IdentityApi } from '../../core/identity-api';
import { CoverageGranularity, computeCoverage, getPeriodRange, shiftPeriod, toDateOnlyString } from '../../core/geo-coverage';
import { FieldSalesEntryItem, OrgUser } from '../../core/models';
import { Loader } from '../../loader/loader';

// Admin-only (route-gated by adminGuard) -- picks one sales rep and a
// day/week/month period, shows every one of that rep's sales-entry locations
// on a map plus a computed coverage area (convex hull). Was previously a card
// embedded in sales-dashboard.ts; extracted verbatim into its own page/route.
@Component({
  selector: 'app-sales-coverage',
  imports: [FormsModule, DecimalPipe, Loader],
  templateUrl: './sales-coverage.html',
  styleUrl: './sales-coverage.css',
})
export class SalesCoverage implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('coverageMap') coverageMapEl?: ElementRef<HTMLDivElement>;
  private map?: L.Map;
  private mapLayers: L.Layer[] = [];

  reps = signal<OrgUser[]>([]);
  errorMessage = signal<string | null>(null);

  coverageRepId = signal('');
  coverageGranularity = signal<CoverageGranularity>('week');
  coverageAnchor = signal(new Date());
  coverageEntries = signal<FieldSalesEntryItem[]>([]);
  coverageLoading = signal(false);
  coverageError = signal<string | null>(null);

  readonly coveragePeriod = computed(() => getPeriodRange(this.coverageGranularity(), this.coverageAnchor()));
  readonly coverageStats = computed(() =>
    computeCoverage(this.coverageEntries().filter((e) => e.latitude != null && e.longitude != null).map((e) => ({ latitude: e.latitude!, longitude: e.longitude! }))),
  );

  constructor(
    private readonly api: CrmApi,
    private readonly identityApi: IdentityApi,
  ) {}

  ngOnInit(): void {
    this.identityApi.getOrganizationUsers().subscribe({
      next: (users) => {
        this.reps.set(users);
        if (!this.coverageRepId() && users.length > 0) {
          this.coverageRepId.set(users[0].id);
          this.loadCoverage();
        }
      },
      error: () => {
        this.errorMessage.set('Could not load the rep list.');
      },
    });
  }

  ngAfterViewInit(): void {
    if (!this.coverageMapEl) {
      return;
    }
    this.map = L.map(this.coverageMapEl.nativeElement, {
      center: [20.5937, 78.9629],
      zoom: 5,
    });
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '&copy; OpenStreetMap contributors',
      maxZoom: 19,
    }).addTo(this.map);
    this.renderCoverageMap();
  }

  ngOnDestroy(): void {
    this.map?.remove();
  }

  setCoverageGranularity(granularity: CoverageGranularity): void {
    this.coverageGranularity.set(granularity);
    this.loadCoverage();
  }

  navigateCoverage(direction: -1 | 1): void {
    this.coverageAnchor.set(shiftPeriod(this.coverageGranularity(), this.coverageAnchor(), direction));
    this.loadCoverage();
  }

  onCoverageRepChange(): void {
    this.loadCoverage();
  }

  private loadCoverage(): void {
    const repId = this.coverageRepId();
    if (!repId) {
      return;
    }

    const { from, to } = this.coveragePeriod();
    this.coverageLoading.set(true);
    this.coverageError.set(null);
    this.api.getSalesEntryLocations(repId, toDateOnlyString(from), toDateOnlyString(to)).subscribe({
      next: (result) => {
        this.coverageEntries.set(result.items);
        this.coverageLoading.set(false);
        this.renderCoverageMap();
      },
      error: () => {
        this.coverageError.set('Could not load coverage for this rep/period.');
        this.coverageLoading.set(false);
      },
    });
  }

  private renderCoverageMap(): void {
    if (!this.map) {
      return;
    }

    for (const layer of this.mapLayers) {
      this.map.removeLayer(layer);
    }
    this.mapLayers = [];

    const points = this.coverageEntries().filter((e) => e.latitude != null && e.longitude != null);
    for (const entry of points) {
      const marker = L.circleMarker([entry.latitude!, entry.longitude!], {
        radius: 7,
        color: '#fc424a',
        fillColor: '#fc424a',
        fillOpacity: 0.85,
      }).bindPopup(`<strong>${entry.name}</strong><br>${new Date(entry.createdAtUtc).toLocaleString()}${entry.products ? `<br>${entry.products}` : ''}`);
      marker.addTo(this.map);
      this.mapLayers.push(marker);
    }

    const hull = this.coverageStats().hull;
    if (hull.length >= 3) {
      const polygon = L.polygon(hull.map((p) => [p.lat, p.lng]), {
        color: '#487fe9',
        weight: 2,
        fillOpacity: 0.12,
      });
      polygon.addTo(this.map);
      this.mapLayers.push(polygon);
    }

    if (points.length > 0) {
      const bounds = L.latLngBounds(points.map((p) => [p.latitude!, p.longitude!] as [number, number]));
      this.map.fitBounds(bounds, { padding: [30, 30], maxZoom: 15 });
    }
  }
}
