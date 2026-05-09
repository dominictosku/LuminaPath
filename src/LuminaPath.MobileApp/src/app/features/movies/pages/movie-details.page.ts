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
  timeOutline,
} from 'ionicons/icons';
import { firstValueFrom } from 'rxjs';
import { Movie } from '../models/movies.model';
import { MovieService } from '../services/movie.service';
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
export class MovieDetailsPage implements OnInit {
  movie: Movie | null = null;
  isLoading = true;
  errorMessage = '';

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly location: Location,
    private readonly movieService: MovieService,
  ) {
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
    return this.formatDate(this.movie?.releaseDate);
  }

  get watchTimeLabel(): string {
    return this.formatMinutes(this.movie?.expectedWatchTimeMinutes);
  }

  get watchedLabel(): string {
    return this.formatMinutes(this.movie?.myMovies?.currentWatchTimeMinutes);
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

    void this.router.navigateByUrl('/media');
  }

  private async loadMovie(movieId: number): Promise<void> {
    try {
      this.movie = await firstValueFrom(this.movieService.get(movieId));
    } catch {
      this.showError('Movie could not be loaded.');
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
