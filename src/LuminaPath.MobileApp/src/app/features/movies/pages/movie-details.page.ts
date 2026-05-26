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
    IonToolbar,
    LibraryCreateDialogComponent,
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
  readonly auth = inject(AuthService);

  /** Catalog dialog needs a MediaModeOption — this page is movies-only. */
  readonly moviesMode: MediaModeOption =
    MEDIA_MODE_OPTIONS.find((option) => option.id === 'movies') ?? MEDIA_MODE_OPTIONS[0];

  movie: Movie | null = null;
  isLoading = true;
  errorMessage = '';

  // Admin edit/delete state ----------------------------------------------------
  isEditDialogOpen = false;
  isSavingCatalogEdit = false;
  editErrorMessage = '';
  editForm: CreateMediaForm = emptyCreateForm();
  isDeletingCatalogEntry = false;

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

  /** Status options bound to the (unused-in-edit-mode) library toggle. */
  get watchStatusOptions() {
    return this.mediaView.statusOptions(this.moviesMode);
  }

  /** Admins and editors can mutate the shared catalog entry from here. */
  get canEditCatalog(): boolean {
    return this.auth.canEditCatalog();
  }

  openEditDialog(): void {
    if (!this.canEditCatalog || !this.movie) return;
    this.editErrorMessage = '';
    this.editForm = this.movieToCreateForm(this.movie);
    this.isEditDialogOpen = true;
  }

  closeEditDialog(): void {
    if (this.isSavingCatalogEdit) return;
    this.isEditDialogOpen = false;
  }

  /** Commit catalog edits via MovieService.put, then refetch the page. */
  async submitCatalogEdit(): Promise<void> {
    if (!this.canEditCatalog || !this.movie || this.isSavingCatalogEdit) return;
    const name = this.editForm.name.trim();
    if (!name) {
      this.editErrorMessage = 'Title is required.';
      return;
    }

    this.isSavingCatalogEdit = true;
    this.editErrorMessage = '';
    const movieId = this.movie.id;

    try {
      // Build the PUT payload from scratch — do NOT spread `this.movie`.
      // Same lesson as the games edit flow: echoing back the navigation
      // collections (myMovies) makes EF try to upsert personal entries
      // and trips FK_MyMovies_AspNetUsers_LuminaUserId.
      const updated = Object.assign(new Movie(), {
        id: movieId,
        name,
        description: this.editForm.description,
        releaseDate: this.editForm.releaseDate || this.movie.releaseDate,
        genre: this.editForm.genre,
        expectedWatchTimeMinutes: this.editForm.expectedWatchTimeMinutes,
        image: this.editForm.cover ?? this.movie.image,
        myMovies: null,
      });

      await firstValueFrom(this.movieService.put(movieId, updated));
      this.isEditDialogOpen = false;
      await this.loadMovie(movieId);
    } catch (error) {
      this.editErrorMessage = extractErrorMessage(
        error,
        'Movie could not be updated.',
      );
    } finally {
      this.isSavingCatalogEdit = false;
    }
  }

  /** Confirm + delete the catalog movie. On success, navigate back to /library. */
  async confirmDeleteCatalogEntry(): Promise<void> {
    if (!this.canEditCatalog || !this.movie || this.isDeletingCatalogEntry) return;

    const libraryWarning = this.isInLibrary
      ? ' It is currently referenced by your personal library entry.'
      : '';

    const alert = await this.alertController.create({
      header: `Delete "${this.movie.name}"?`,
      message:
        `This removes the shared catalog movie, including its metadata and cover link.${libraryWarning} This cannot be undone.`,
      cssClass: 'media-confirm-alert',
      buttons: [
        { text: 'Keep', role: 'cancel' },
        {
          text: 'Delete',
          role: 'destructive',
          cssClass: 'media-confirm-alert__destructive',
          handler: () => {
            void this.deleteCatalogEntry();
          },
        },
      ],
    });
    await alert.present();
  }

  private async deleteCatalogEntry(): Promise<void> {
    if (!this.canEditCatalog || !this.movie || this.isDeletingCatalogEntry) return;
    const movieId = this.movie.id;
    this.isDeletingCatalogEntry = true;

    try {
      await firstValueFrom(this.movieService.delete(movieId));
      this.mediaStore.removeItem(movieId);
      void this.router.navigateByUrl('/library');
    } catch (error) {
      this.errorMessage = extractErrorMessage(error, 'Movie could not be deleted.');
    } finally {
      this.isDeletingCatalogEntry = false;
    }
  }

  /** Maps the loaded Movie into the create-dialog form draft for editing. */
  private movieToCreateForm(movie: Movie): CreateMediaForm {
    const blank = emptyCreateForm();
    return {
      ...blank,
      name: movie.name ?? '',
      description: movie.description ?? '',
      releaseDate: this.toDateInputValue(movie.releaseDate),
      genre: movie.genre ?? '',
      expectedWatchTimeMinutes: movie.expectedWatchTimeMinutes ?? null,
      createLibraryEntry: false,
      cover: movie.image ?? null,
      coverPreviewUrl: null,
    };
  }

  /** Coerce the Movie's releaseDate (Date | string) into a YYYY-MM-DD value. */
  private toDateInputValue(value: Date | string | null | undefined): string {
    if (!value) return '';
    const date = value instanceof Date ? value : new Date(value);
    if (Number.isNaN(date.getTime())) return '';
    const y = date.getFullYear();
    const m = String(date.getMonth() + 1).padStart(2, '0');
    const d = String(date.getDate()).padStart(2, '0');
    return `${y}-${m}-${d}`;
  }
}
