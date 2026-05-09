import { CommonModule, Location } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
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
  filmOutline,
  libraryOutline,
  sparklesOutline,
  timeOutline,
} from 'ionicons/icons';
import { firstValueFrom } from 'rxjs';
import { Anime } from '../models/animes.model';
import { AnimeService } from '../services/anime.service';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';

const WATCH_STATUS_LABELS: Record<number, string> = {
  0: 'On hold',
  1: 'Planned',
  2: 'Watching',
  3: 'Completed',
  4: 'Dropped',
};

@Component({
  selector: 'app-anime-details',
  templateUrl: './anime-details.page.html',
  styleUrls: ['./anime-details.page.scss'],
  imports: [
    CommonModule,
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
export class AnimeDetailsPage implements OnInit {
  anime: Anime | null = null;
  isLoading = true;
  errorMessage = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly location: Location,
    private readonly animeService: AnimeService,
  ) {
    addIcons({
      arrowBackOutline,
      calendarClearOutline,
      filmOutline,
      libraryOutline,
      sparklesOutline,
      timeOutline,
    });
  }

  async ngOnInit(): Promise<void> {
    const animeId = Number(this.route.snapshot.paramMap.get('animeId'));
    if (!Number.isInteger(animeId) || animeId <= 0) {
      this.showError('Anime not found.');
      return;
    }

    await this.loadAnime(animeId);
  }

  get isInLibrary(): boolean {
    return !!this.anime?.myAnimes;
  }

  get libraryStatusLabel(): string {
    return this.isInLibrary ? 'Already in your library' : 'Not in library';
  }

  get statusLabel(): string {
    const status = this.anime?.myAnimes?.status;
    return status == null ? 'Catalog' : WATCH_STATUS_LABELS[Number(status)] ?? 'Catalog';
  }

  get releaseLabel(): string {
    return this.formatDate(this.anime?.releaseDate);
  }

  get watchTimeLabel(): string {
    return this.formatMinutes(this.anime?.expectedWatchTimeMinutes);
  }

  get episodeLabel(): string {
    const current = Number(this.anime?.myAnimes?.currentEpisode) || 0;
    const total = Number(this.anime?.episodeCount) || 0;
    if (total <= 0) {
      return current > 0 ? `Episode ${current}` : 'No episode count';
    }
    return this.isInLibrary ? `Episode ${current}/${total}` : `${total} episodes`;
  }

  get progress(): number {
    const total = Number(this.anime?.episodeCount) || 0;
    const current = Number(this.anime?.myAnimes?.currentEpisode) || 0;
    return total > 0 ? Math.min(100, Math.round((current / total) * 100)) : 0;
  }

  imageUrl(): string {
    return mediaImageUrl(this.anime?.image);
  }

  goBack(): void {
    if (window.history.length > 1) {
      this.location.back();
      return;
    }

    void this.router.navigateByUrl('/library');
  }

  private async loadAnime(animeId: number): Promise<void> {
    try {
      this.anime = await firstValueFrom(this.animeService.get(animeId));
    } catch {
      this.showError('Anime could not be loaded.');
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
