import { AfterViewInit, Component, ElementRef, OnDestroy, effect, input, viewChild } from '@angular/core';
import { Chart, registerables } from 'chart.js';
import { ChartPoint } from './chart-line';

Chart.register(...registerables);

// Near-identical to ChartLine (same input/effect/lifecycle shape) -- only the Chart.js
// `type` differs. Kept as a separate small component rather than a `type` input on
// ChartLine itself, matching this codebase's existing preference for a few near-duplicate
// small files over one config-driven component (see CsvExporter/XlsxExporter/PdfExporter).
@Component({
  selector: 'app-chart-bar',
  templateUrl: './chart-bar.html',
  styleUrl: './chart-bar.css',
})
export class ChartBar implements AfterViewInit, OnDestroy {
  points = input<ChartPoint[]>([]);
  seriesLabel = input('');

  private readonly canvasRef = viewChild<ElementRef<HTMLCanvasElement>>('canvas');
  private chart: Chart | null = null;
  private viewReady = false;

  constructor() {
    effect(() => {
      const points = this.points();
      this.seriesLabel();
      if (this.viewReady) {
        this.render(points);
      }
    });
  }

  ngAfterViewInit(): void {
    this.viewReady = true;
    this.render(this.points());
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  private render(points: ChartPoint[]): void {
    const canvas = this.canvasRef()?.nativeElement;
    if (!canvas) {
      return;
    }

    this.chart?.destroy();

    const accent = getComputedStyle(document.documentElement).getPropertyValue('--accent').trim() || '#e66239';

    this.chart = new Chart(canvas, {
      type: 'bar',
      data: {
        labels: points.map((p) => p.label),
        datasets: [
          {
            label: this.seriesLabel(),
            data: points.map((p) => p.value),
            backgroundColor: accent,
            borderRadius: 4,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
          y: { beginAtZero: true, ticks: { precision: 0 } },
        },
      },
    });
  }
}
