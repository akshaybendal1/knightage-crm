import { AfterViewInit, Component, ElementRef, OnDestroy, effect, input, viewChild } from '@angular/core';
import { Chart, registerables } from 'chart.js';
import { ChartPoint } from './chart-line';

Chart.register(...registerables);

// A fixed palette, unlike ChartBar/ChartLine's single --accent color -- a pie needs one
// distinct color per slice. Cycles if there are more points than colors.
const SLICE_COLORS = ['#e66239', '#4c8bf5', '#2fbf71', '#f5b942', '#a05af5', '#f5426c', '#42c8f5', '#8a8f98'];

@Component({
  selector: 'app-chart-pie',
  templateUrl: './chart-pie.html',
  styleUrl: './chart-pie.css',
})
export class ChartPie implements AfterViewInit, OnDestroy {
  points = input<ChartPoint[]>([]);

  private readonly canvasRef = viewChild<ElementRef<HTMLCanvasElement>>('canvas');
  private chart: Chart | null = null;
  private viewReady = false;

  constructor() {
    effect(() => {
      const points = this.points();
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

    this.chart = new Chart(canvas, {
      type: 'pie',
      data: {
        labels: points.map((p) => p.label),
        datasets: [
          {
            data: points.map((p) => p.value),
            backgroundColor: points.map((_, i) => SLICE_COLORS[i % SLICE_COLORS.length]),
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: true, position: 'right' } },
      },
    });
  }
}
