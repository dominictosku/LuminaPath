import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';

import { AuthService } from 'src/app/core/auth/services/auth.service';
import { MediaMode, MediaModeService } from 'src/app/shared/services/media-mode.service';
import { extractErrorMessage } from 'src/app/shared/utils/extract-error';
import { capitalize } from 'src/app/shared/utils/format';
import { normalizeIsoDate, parseDateOrNull } from 'src/app/shared/utils/date-helpers';
import { Game } from 'src/app/features/games/models/games.model';
import { Anime } from 'src/app/features/animes/models/animes.model';
import { Series } from 'src/app/features/series/models/series.model';
import { GameService } from 'src/app/features/games/services/game.service';
import { AnimeService } from 'src/app/features/animes/services/anime.service';
import { SeriesService } from 'src/app/features/series/services/series.service';

import { MediaStore } from '../state/media.store';
import { CreateMediaForm, emptyCreateForm } from '../components/library-create-dialog/library-create-dialog.model';

/**
 * Admin-only "create a catalog entry" flow. Owns the create dialog's state and
 * dispatches to the right typed service per media mode, optionally chaining a
 * personal library entry. Provided at the page level.
 */
@Injectable()
export class LibraryCatalogCreator {
  private readonly auth = inject(AuthService);
  private readonly mediaModeService = inject(MediaModeService);
  private readonly mediaStore = inject(MediaStore);
  private readonly gameService = inject(GameService);
  private readonly animeService = inject(AnimeService);
  private readonly seriesService = inject(SeriesService);

  /** Only games / animes / series are creatable from this dialog. */
  private static readonly CREATABLE_KINDS: ReadonlySet<MediaMode> = new Set(['games', 'animes', 'series']);

  isOpen = false;
  isSaving = false;
  errorMessage = '';
  form: CreateMediaForm = emptyCreateForm();

  /** Visible only for admins in a creatable media mode. */
  get canCreate(): boolean {
    return this.auth.isAdmin() && LibraryCatalogCreator.CREATABLE_KINDS.has(this.mediaModeService.mode().id);
  }

  open(): void {
    if (!this.canCreate) return;
    this.errorMessage = '';
    this.form = emptyCreateForm();
    this.isOpen = true;
  }

  close(): void {
    if (this.isSaving) return;
    this.isOpen = false;
  }

  /** Create the catalog entry (and optional library entry), then invoke
   *  `onCreated` so the page can show feedback and refresh the list. */
  async submit(onCreated: (name: string) => void): Promise<void> {
    if (!this.canCreate || this.isSaving) return;

    const name = this.form.name.trim();
    if (!name) {
      this.errorMessage = 'Title is required.';
      return;
    }

    this.isSaving = true;
    this.errorMessage = '';

    try {
      const created = await this.createCatalogEntry(name);
      if (this.form.createLibraryEntry && created.id > 0) {
        await this.mediaStore.addToLibrary(created.id, {
          status: this.form.libraryEntry.status,
          timeSpend: this.form.libraryEntry.timeSpend ?? 0,
          rating: this.form.libraryEntry.rating,
          startDate: normalizeIsoDate(this.form.libraryEntry.startDate),
          endDate: normalizeIsoDate(this.form.libraryEntry.endDate),
          personalNotes: this.form.libraryEntry.personalNotes,
          currentEpisode: this.form.libraryEntry.currentEpisode,
        });
      }
      this.isOpen = false;
      onCreated(name);
    } catch (error) {
      this.errorMessage = extractErrorMessage(
        error,
        `${capitalize(this.mediaModeService.mode().singular)} could not be created.`,
      );
    } finally {
      this.isSaving = false;
    }
  }

  /** Dispatch to the right typed service per media mode, returning the
   *  freshly-created entity (so we can chain MyGame/MyAnime/MySeries). */
  private async createCatalogEntry(name: string): Promise<{ id: number }> {
    const releaseDate = parseDateOrNull(this.form.releaseDate);
    const cover = this.form.cover;
    switch (this.mediaModeService.mode().id) {
      case 'games': {
        const game = Object.assign(new Game(), {
          name,
          description: this.form.description,
          releaseDate: releaseDate ?? new Date(),
          genre: this.form.genre,
          platforms: this.form.platforms,
          playtime: this.form.playtime ?? 0,
          image: cover,
        });
        return firstValueFrom(this.gameService.post(game));
      }
      case 'animes': {
        const anime = Object.assign(new Anime(), {
          name,
          description: this.form.description,
          releaseDate: this.form.releaseDate || null,
          genre: this.form.genre,
          episodeCount: this.form.episodeCount,
          expectedWatchTimePerEpisodeMinutes: this.form.expectedWatchTimePerEpisodeMinutes,
          expectedWatchTimeMinutes: this.form.expectedWatchTimeMinutes,
          image: cover,
        });
        return firstValueFrom(this.animeService.post(anime));
      }
      case 'series': {
        const series = Object.assign(new Series(), {
          name,
          description: this.form.description,
          releaseDate: this.form.releaseDate || null,
          genre: this.form.genre,
          episodeCount: this.form.episodeCount,
          expectedWatchTimePerEpisodeMinutes: this.form.expectedWatchTimePerEpisodeMinutes,
          expectedWatchTimeMinutes: this.form.expectedWatchTimeMinutes,
          image: cover,
        });
        return firstValueFrom(this.seriesService.post(series));
      }
      default:
        throw new Error(`Create not supported for ${this.mediaModeService.mode().id}.`);
    }
  }
}
