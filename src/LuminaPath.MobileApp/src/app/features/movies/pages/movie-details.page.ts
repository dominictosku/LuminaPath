import { Location } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
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
  timeOutline,
} from 'ionicons/icons';
import { firstValueFrom } from 'rxjs';
import { Movie } from '../models/movies.model';
import { MovieService } from '../services/movie.service';
import { MediaStore } from 'src/app/features/library/state/media.store';
import { formatHoursMinutes, formatShortDate } from 'src/app/shared/utils/format';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';

const WATCH_STATUS_LABELS: Record<number, string> = {
  0: 'On hold',
  1: 'Planned',
  2: 'Watching',
  3: 'Completed',
  4: 'Dropped',
};

@Component({
  selector: 'app-movie-details',
  templateUrl: './movie-details.page.html',
  styleUrls: ['./movie-details.page.scss'],
  imports: [
    IonBadge,
    IonButton,
    IonButtons,
    IonContent,
    IonHeader,
    IonIcon,
    IonSpinner,
    IonTitle,
    IonToolbar
],
})
export class MovieDetailsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly movieService = inject(MovieService);
  private readonly mediaStore = inject(MediaStore);

  movie: Movie | null = null;
  isLoading = true;
  errorMessage = '';

  constructor() {
    addIcons({
      arrowBackOutline,
      calendarClearOutline,
      filmOutline,
      libraryOutline,
      timeOutline,
    });
  }

  async ngOnInit(): Promise<void> {
    const movieId = Number(this.route.snapshot.paramMap.get('movieId'));
    if (!Number.isInteger(movieId) || movieId <= 0) {
      this.showError('Movie not found.');
      return;
    }

    await this.loadMovie(movieId);
  }

  get isInLibrary(): boolean {
    return !!this.movie?.myMovies;
  }

  get libraryStatusLabel(): string {
    return this.isInLibrary ? 'Already in your library' : 'Not in library';
  }

  get statusLabel(): string {
    const status = this.movie?.myMovies?.status;
    return status == null ? 'Catalog' : WATCH_STATUS_LABELS[Number(status)] ?? 'Catalog';
  }

  get releaseLabel(): string {
    return formatShortDate(this.movie?.releaseDate);
  }

  get watchTimeLabel(): string {
    return formatHoursMinutes(this.movie?.expectedWatchTimeMinutes);
  }

  get watchedLabel(): string {
    return formatHoursMinutes(this.movie?.myMovies?.currentWatchTimeMinutes);
  }

  get progress(): number {
    const expected = Number(this.movie?.expectedWatchTimeMinutes) || 0;
    const watched = Number(this.movie?.myMovies?.currentWatchTimeMinutes) || 0;
    return expected > 0 ? Math.min(100, Math.round((watched / expected) * 100)) : 0;
  }

  imageUrl(): string {
    return mediaImageUrl(this.movie?.image);
  }

  goBack(): void {
    if (window.history.length > 1) {
      this.location.back();
      return;
    }

    void this.router.navigateByUrl('/library');
  }

  private async loadMovie(movieId: number): Promise<void> {
    try {
      this.movie = await firstValueFrom(this.movieService.get(movieId));
      this.syncMediaStore(movieId);
    } catch {
      this.showError('Movie could not be loaded.');
    } finally {
      this.isLoading = false;
    }
  }

  private syncMediaStore(movieId: number): void {
    const entry = this.movie?.myMovies ?? null;
    this.mediaStore.setLibraryEntry(movieId, entry ? {
      id: entry.id,
      rating: entry.rating,
      startDate: entry.startDate,
      endDate: entry.endDate,
      status: Number(entry.status ?? 1),
      timeSpend: entry.timeSpend,
      currentWatchTimeMinutes: entry.currentWatchTimeMinutes,
    } : null);
  }

  private showError(message: string): void {
    this.errorMessage = message;
    this.isLoading = false;
  }
}
