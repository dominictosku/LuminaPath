import { Location } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import {
  AlertController,
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
import { firstValueFrom } from 'rxjs';
import { Movie } from '../models/movies.model';
import { MovieService } from '../services/movie.service';
import { AuthService } from 'src/app/core/auth/services/auth.service';
import { MediaStore } from 'src/app/features/library/state/media.store';
import { CatalogEditController } from 'src/app/features/library/services/catalog-edit.controller';
import { LibraryCreateDialogComponent } from 'src/app/features/library/components/library-create-dialog/library-create-dialog.component';
import {
  CreateMediaForm,
  emptyCreateForm,
} from 'src/app/features/library/components/library-create-dialog/library-create-dialog.model';
import { MEDIA_MODE_OPTIONS, MediaModeOption } from 'src/app/shared/services/media-mode.service';
import { MediaLibraryViewService } from 'src/app/features/library/services/media-library-view.service';
import { RequestCache } from 'src/app/shared/services/request-cache.service';
import { extractErrorMessage } from 'src/app/shared/utils/extract-error';
import { formatHoursMinutes, formatShortDate } from 'src/app/shared/utils/format';
import { toDateInputValue } from 'src/app/shared/utils/date-helpers';
import { goBackOrHome } from 'src/app/shared/utils/navigation';
import { watchStatusLabel } from 'src/app/features/library/models/library-status.model';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { DetailTabOption, DetailTabsComponent } from 'src/app/shared/components/detail-tabs/detail-tabs.component';
import { MediaAvailabilityComponent } from 'src/app/shared/components/media-availability/media-availability.component';
import { MediaVideosComponent } from 'src/app/shared/components/media-videos/media-videos.component';
import { CompletionCardService } from 'src/app/shared/services/completion-card.service';

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
    IonToolbar,
    DetailTabsComponent,
    LibraryCreateDialogComponent,
    MediaAvailabilityComponent,
    MediaVideosComponent,
],
})
export class MovieDetailsPage implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly movieService = inject(MovieService);
  private readonly mediaStore = inject(MediaStore);
  private readonly alertController = inject(AlertController);
  private readonly mediaView = inject(MediaLibraryViewService);
  private readonly cache = inject(RequestCache);
  private readonly completionCard = inject(CompletionCardService);
  readonly auth = inject(AuthService);

  /** Catalog dialog needs a MediaModeOption — this page is movies-only. */
  readonly moviesMode: MediaModeOption =
    MEDIA_MODE_OPTIONS.find((option) => option.id === 'movies') ?? MEDIA_MODE_OPTIONS[0];
  readonly detailTabs: readonly DetailTabOption[] = [
    { value: 'overview', label: 'Overview' },
    { value: 'gallery', label: 'Gallery' },
  ];

  movie: Movie | null = null;
  isLoading = true;
  errorMessage = '';
  selectedTab: 'overview' | 'gallery' = 'overview';
  completionCardMessage = '';

  /** Owns the admin edit-dialog + delete-confirm flow for the catalog movie. */
  readonly catalogEdit = new CatalogEditController<Movie>(
    this.alertController,
    this.mediaStore,
    this.router,
    {
      canEdit: () => this.canEditCatalog,
      entity: () => this.movie,
      id: (movie) => movie.id,
      name: (movie) => movie.name,
      isInLibrary: () => this.isInLibrary,
      toForm: (movie) => this.movieToCreateForm(movie),
      saveEdit: (movie, name, form) => this.movieService.put(movie.id, this.buildCatalogPut(movie, name, form)),
      deleteEntity: (movie) => this.movieService.delete(movie.id),
      reload: (movie) => this.loadMovie(movie.id),
      reportError: (message) => { this.errorMessage = message; },
      labels: { singular: 'movie' },
    },
  );

  async ngOnInit(): Promise<void> {
    const movieId = Number(this.route.snapshot.paramMap.get('movieId'));
    if (!Number.isInteger(movieId) || movieId <= 0) {
      this.showError('Movie not found.');
      return;
    }

    this.selectedTab = 'overview';
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
    return status == null ? 'Catalog' : watchStatusLabel(Number(status));
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

  get canShareCompletionCard(): boolean {
    return this.isInLibrary && Number(this.movie?.myMovies?.status ?? -1) === 3;
  }

  imageUrl(): string {
    return mediaImageUrl(this.movie?.image);
  }

  goBack(): void {
    goBackOrHome(this.location, this.router);
  }

  setDetailTab(value: unknown): void {
    this.selectedTab = value === 'gallery' ? 'gallery' : 'overview';
  }

  async exportCompletionCard(): Promise<void> {
    if (!this.movie || !this.canShareCompletionCard) {
      return;
    }

    this.completionCardMessage = '';
    try {
      const fileName = await this.completionCard.export({
        title: this.movie.name,
        kindLabel: 'Movie',
        statusLabel: this.statusLabel,
        coverUrl: this.imageUrl(),
        rating: this.movie.myMovies?.rating ?? null,
        completedAt: this.movie.myMovies?.endDate ?? null,
        detailLines: [
          this.releaseLabel,
          this.watchTimeLabel,
          this.watchedLabel,
        ],
      });
      this.completionCardMessage = `Saved ${fileName}.`;
    } catch (error) {
      this.completionCardMessage = extractErrorMessage(error, 'Completion card could not be exported.');
    }
  }

  private async loadMovie(movieId: number): Promise<void> {
    // Cache-first paint so detail deep links work offline. Refetch in
    // the background and replace on success; on failure keep the cache.
    const cacheKey = `media:movies:${movieId}`;
    const cached = this.cache.get<Movie>(cacheKey);
    if (cached) {
      this.movie = cached;
      this.syncMediaStore(movieId);
      this.isLoading = false;
    }

    try {
      const fresh = await firstValueFrom(this.movieService.get(movieId));
      this.movie = fresh;
      this.cache.set(cacheKey, fresh);
      this.syncMediaStore(movieId);
    } catch {
      if (!cached) {
        this.showError('Movie could not be loaded.');
      }
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

  // ---------------------------------------------------------------------------
  // Admin / editor catalog actions
  // ---------------------------------------------------------------------------

  /** Status options passed through the shared catalog dialog input contract. */
  get watchStatusOptions() {
    return this.mediaView.statusOptions(this.moviesMode);
  }

  /** Admins and editors can mutate the shared catalog entry from here. */
  get canEditCatalog(): boolean {
    return this.auth.canEditCatalog();
  }

  /**
   * Build the PUT payload from scratch — do NOT spread `this.movie`.
   * Echoing back the navigation collections (myMovies) makes EF try to
   * upsert personal entries and trips FK_MyMovies_AspNetUsers_LuminaUserId.
   */
  private buildCatalogPut(movie: Movie, name: string, form: CreateMediaForm): Movie {
    return Object.assign(new Movie(), {
      id: movie.id,
      name,
      description: form.description,
      releaseDate: form.releaseDate || movie.releaseDate,
      genre: form.genre,
      expectedWatchTimeMinutes: form.expectedWatchTimeMinutes,
      image: form.cover ?? movie.image,
      myMovies: null,
    });
  }

  /** Maps the loaded Movie into the create-dialog form draft for editing. */
  private movieToCreateForm(movie: Movie): CreateMediaForm {
    const blank = emptyCreateForm();
    return {
      ...blank,
      name: movie.name ?? '',
      description: movie.description ?? '',
      releaseDate: toDateInputValue(movie.releaseDate),
      genre: movie.genre ?? '',
      expectedWatchTimeMinutes: movie.expectedWatchTimeMinutes ?? null,
      createLibraryEntry: false,
      cover: movie.image ?? null,
      coverPreviewUrl: null,
    };
  }
}
