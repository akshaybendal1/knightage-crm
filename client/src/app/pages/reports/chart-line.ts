import { AfterViewInit, Component, ElementRef, OnDestroy, effect, input, viewChild } from '@angular/core';
import { Chart, registerables } from 'chart.js';

Chart.register(...registerables);

export interface ChartPoint {
  label: string;
  value: number;
}

@Component({
  selector: 'app-chart-line',
  templateUrl: './chart-line.html',
  styleUrl: './chart-line.css',
})
export class ChartLine implements AfterViewInit, OnDestroy {
  points = input<ChartPoint[]>([]);
  seriesLabel = input('');

  private readonly canvasRef = viewChild<ElementRef<HTMLCanvasElement>>('canvas');
  private chart: Chart | null = null;
  private viewReady = false;

  constructor() {
    // Re-renders on every points/seriesLabel change once the view exists. The first
    // run (component construction, before ngAfterViewInit) is a deliberate no-op --
    // there's no canvas to draw on yet, ngAfterViewInit does that initial render.
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
      type: 'line',
      data: {
        labels: points.map((p) => p.label),
        datasets: [
          {
            label: this.seriesLabel(),
            data: points.map((p) => p.value),
            borderColor: accent,
            backgroundColor: `${accent}33`,
            tension: 0.3,
            fill: true,
            pointRadius: 3,
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
