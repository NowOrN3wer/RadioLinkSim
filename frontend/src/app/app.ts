import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { ReactiveFormsModule, Validators, FormBuilder } from '@angular/forms';
import { finalize } from 'rxjs';
import * as L from 'leaflet';
import { ButtonModule } from 'primeng/button';
import { CardModule } from 'primeng/card';
import { ChartModule } from 'primeng/chart';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageModule } from 'primeng/message';

interface LinkProfileRequest {
  latA: number;
  lonA: number;
  latB: number;
  lonB: number;
  stepMeters: number;
}

interface ProfilePoint {
  latitude: number;
  longitude: number;
  distanceFromAMeters: number;
  elevationMeters: number;
}

interface LinkProfileResponse {
  greatCircleDistanceMeters: number;
  effectiveDistanceMeters: number;
  stepMeters: number;
  pointCount: number;
  points: ProfilePoint[];
}

@Component({
  selector: 'app-root',
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    CardModule,
    ChartModule,
    InputNumberModule,
    MessageModule,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements AfterViewInit, OnDestroy {
  @ViewChild('map') private mapElement!: ElementRef<HTMLDivElement>;

  private readonly http = inject(HttpClient);
  private readonly formBuilder = inject(FormBuilder);
  private map?: L.Map;
  private routeLayer = L.layerGroup();

  protected readonly loading = signal(false);
  protected readonly errorMessage = signal('');
  protected readonly profile = signal<LinkProfileResponse | null>(null);
  protected readonly chartData = signal({ labels: [] as number[], datasets: [] as object[] });

  protected readonly chartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    interaction: { intersect: false, mode: 'index' as const },
    plugins: {
      legend: { display: false },
      tooltip: {
        callbacks: {
          title: (items: { label: string }[]) => `${Number(items[0].label).toFixed(2)} km`,
          label: (item: { parsed: { y: number } }) => `Yükseklik: ${item.parsed.y.toFixed(1)} m`,
        },
      },
    },
    scales: {
      x: { title: { display: true, text: "A'dan itibaren kümülatif mesafe (km)" } },
      y: { title: { display: true, text: 'Yükseklik (m)' } },
    },
  };

  protected readonly form = this.formBuilder.nonNullable.group({
    latA: [41.0082, [Validators.required, Validators.min(-90), Validators.max(90)]],
    lonA: [28.9784, [Validators.required, Validators.min(-180), Validators.max(180)]],
    latB: [39.9334, [Validators.required, Validators.min(-90), Validators.max(90)]],
    lonB: [32.8597, [Validators.required, Validators.min(-180), Validators.max(180)]],
    stepMeters: [5000, [Validators.required, Validators.min(10), Validators.max(5000)]],
  });

  ngAfterViewInit(): void {
    this.map = L.map(this.mapElement.nativeElement, { zoomControl: true });
    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      attribution: '&copy; OpenStreetMap katkıda bulunanlar',
    }).addTo(this.map);
    this.routeLayer.addTo(this.map);
    this.drawRoute(this.form.getRawValue());
  }

  ngOnDestroy(): void {
    this.map?.remove();
  }

  protected calculate(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      this.errorMessage.set('Lütfen koordinatları ve adım mesafesini kontrol edin.');
      return;
    }

    const request = this.form.getRawValue();
    this.loading.set(true);
    this.errorMessage.set('');
    this.drawRoute(request);

    this.http
      .post<LinkProfileResponse>('/api/link-profile', request)
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: (response) => {
          this.profile.set(response);
          this.chartData.set({
            labels: response.points.map((point) => point.distanceFromAMeters / 1000),
            datasets: [
              {
                data: response.points.map((point) => point.elevationMeters),
                borderColor: '#3b82f6',
                backgroundColor: 'rgba(59, 130, 246, 0.16)',
                fill: true,
                tension: 0.25,
                pointRadius: response.pointCount > 100 ? 0 : 2,
              },
            ],
          });
        },
        error: (error) => {
          const detail = error.error?.detail ?? error.error?.title;
          this.errorMessage.set(detail ?? 'Profil hesaplanırken servise ulaşılamadı.');
        },
      });
  }

  private drawRoute(request: LinkProfileRequest): void {
    if (!this.map) return;

    const pointA = L.latLng(request.latA, request.lonA);
    const pointB = L.latLng(request.latB, request.lonB);
    this.routeLayer.clearLayers();

    L.circleMarker(pointA, {
      radius: 9,
      color: '#ffffff',
      weight: 3,
      fillColor: '#2563eb',
      fillOpacity: 1,
    }).bindTooltip('A', { permanent: true, direction: 'top' }).addTo(this.routeLayer);

    L.circleMarker(pointB, {
      radius: 9,
      color: '#ffffff',
      weight: 3,
      fillColor: '#f97316',
      fillOpacity: 1,
    }).bindTooltip('B', { permanent: true, direction: 'top' }).addTo(this.routeLayer);

    L.polyline([pointA, pointB], { color: '#0f172a', weight: 3, dashArray: '8 7' })
      .addTo(this.routeLayer);

    this.map.fitBounds(L.latLngBounds(pointA, pointB).pad(0.18));
  }
}
