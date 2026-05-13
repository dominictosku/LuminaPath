import { CommonModule, Location } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  IonBadge,
  IonButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonIcon,
  IonSpinner,
  IonTitle,
  IonToolbar,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  arrowBackOutline,
  calendarClearOutline,
  layersOutline,
  libraryOutline,
  returnUpBackOutline,
  timeOutline,
  tvOutline,
} from 'ionicons/icons';
import { firstValueFrom } from 'rxjs';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { Series, SeriesSummary } from '../models/series.model';
import { SeriesService } from '../services/series.service';

const WATCH_STATUS_LABELS: Record<number, string> = {
  0: 'On hold',
  1: 'Planned',
  2: 'Watching',
  3: 'Completed',
  4: 'Dropped',
};

@Component({
  selector: 'app-series-details',
  templateUrl: './series-details.page.html',
  styleUrls: ['./series-details.page.scss'],
  imports: [
    CommonModule,
    RouterLink,
    IonBadge,
    IonButton,
    IonButtons,
    IonContent,
    IonHeader,
    IonIcon,
    IonSpinner,
    IonTitle,
    IonToolbar,
  ],
})
export class SeriesDetailsPage implements OnInit {
  series: Series | null = null;
  isLoading = true;
  errorMessage = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly location: Location,
    private readonly seriesService: SeriesService,
  ) {
    addIcons({
      arrowBackOutline,
      calendarClearOutline,
      layersOutline,
      libraryOutline,
      returnUpBackOutline,
      timeOutline,
      tvOutline,
    });
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(async (params) => {
      const seriesId = Number(params.get('seriesId'));
      if (!Number.isInteger(seriesId) || seriesId <= 0) {
        this.showError('Series not found.');
        return;
      }

      this.isLoading = true;
      this.errorMessage = '';
      await this.loadSeries(seriesId);
    });
  }

  get isInLibrary(): boolean {
    return !!this.series?.mySeries;
  }

  get libraryStatusLabel(): string {
    return this.isInLibrary ? 'Already in your library' : 'Not in library';
  }

  get statusLabel(): string {
    const status = this.series?.mySeries?.status;
    return status == null ? 'Catalog' : WATCH_STATUS_LABELS[Number(status)] ?? 'Catalog';
  }

  get releaseLabel(): string {
    return this.formatDate(this.series?.releaseDate);
  }

  get watchTimeLabel(): string {
    return this.formatMinutes(this.series?.expectedWatchTimeMinutes);
  }

  get episodeTimeLabel(): string {
    const value = this.series?.expectedWatchTimePerEpisodeMinutes;
    return value == null ? 'No episode estimate' : `${this.formatMinutes(value)} / episode`;
  }

  get episodeLabel(): string {
    const current = Number(this.series?.mySeries?.currentEpisode) || 0;
    const total = Number(this.series?.episodeCount) || 0;
    if (total <= 0) {
      return current > 0 ? `Episode ${current}` : 'No episode count';
    }
    return this.isInLibrary ? `Episode ${current}/${total}` : `${total} episodes`;
  }

  get progress(): number {
    const total = Number(this.series?.episodeCount) || 0;
    const current = Number(this.series?.mySeries?.currentEpisode) || 0;
    return total > 0 ? Math.min(100, Math.round((current / total) * 100)) : 0;
  }

  get seasons(): SeriesSummary[] {
    return this.series?.seasons ?? [];
  }

  get hasParent(): boolean {
    return !!this.series?.parentSeriesId;
  }

  imageUrl(): string {
    return mediaImageUrl(this.series?.image);
  }

  seasonImageUrl(season: SeriesSummary): string {
    return mediaImageUrl(season.image ?? null);
  }

  seasonReleaseLabel(season: SeriesSummary): string {
    return this.formatDate(season.releaseDate);
  }

  trackBySeason(_: number, season: SeriesSummary): number {
    return season.id;
  }

  goBack(): void {
    if (window.history.length > 1) {
      this.location.back();
      return;
    }

    void this.router.navigateByUrl('/library');
  }

  private async loadSeries(seriesId: number): Promise<void> {
    try {
      this.series = await firstValueFrom(this.seriesService.get(seriesId));
    } catch {
      this.showError('Series could not be loaded.');
    } finally {
      this.isLoading = false;
    }
  }

  private formatDate(value: Date | string | null | undefined): string {
    if (!value) {
      return 'No release date';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return 'No release date';
    }

    return new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric', year: 'numeric' }).format(date);
  }

  private formatMinutes(value: number | null | undefined): string {
    const minutes = Number(value) || 0;
    if (minutes <= 0) {
      return 'No estimate';
    }

    const hours = Math.floor(minutes / 60);
    const remaining = minutes % 60;
    if (hours > 0 && remaining > 0) {
      return `${hours}h ${remaining}m`;
    }
    return hours > 0 ? `${hours}h` : `${remaining}m`;
  }

  private showError(message: string): void {
    this.errorMessage = message;
    this.isLoading = false;
  }
}
