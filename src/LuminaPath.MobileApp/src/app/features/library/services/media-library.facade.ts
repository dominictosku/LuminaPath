import { Injectable, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import { PaginateResult } from 'src/app/core/entities/paginatedResult';
import { MediaFilter } from 'src/app/core/entities/mediaFilter';
import { MediaMode, MediaModeService } from 'src/app/shared/services/media-mode.service';
import { Game } from '../../games/models/games.model';
import { GameService } from '../../games/services/game.service';
import { MyGameService } from '../../my-games/services/my-game.service';
import { Anime, MyAnime } from '../../animes/models/animes.model';
import { AnimeService } from '../../animes/services/anime.service';
import { MyAnimeService } from '../../animes/services/my-anime.service';
import { Movie, MyMovie } from '../../movies/models/movies.model';
import { MovieService } from '../../movies/services/movie.service';
import { MyMovieService } from '../../movies/services/my-movie.service';
import { Series, MySeries } from '../../series/models/series.model';
import { SeriesService } from '../../series/services/series.service';
import { MySeriesService } from '../../series/services/my-series.service';
import { LibraryEntryDetails, MediaItem, UserMediaEntry } from '../models/media-item.model';

type MediaStrategy = {
  list(filter?: MediaFilter): Observable<PaginateResult<MediaItem>>;
  add(mediaId: number, details: LibraryEntryDetails): Observable<UserMediaEntry>;
  update(libraryEntryId: number, mediaId: number, details: LibraryEntryDetails): Observable<UserMediaEntry>;
};

@Injectable({
  providedIn: 'root',
})
export class MediaLibraryFacade {
  private readonly mediaMode = inject(MediaModeService);

  private readonly strategies: Record<MediaMode, MediaStrategy>;

  constructor() {
    const gameService = inject(GameService);
    const myGameService = inject(MyGameService);
    const animeService = inject(AnimeService);
    const myAnimeService = inject(MyAnimeService);
    const movieService = inject(MovieService);
    const myMovieService = inject(MyMovieService);
    const seriesService = inject(SeriesService);
    const mySeriesService = inject(MySeriesService);

    this.strategies = {
      games: {
        list: (filter) => gameService.getAll(filter).pipe(map((page) => this.mapPage(page, (game) => this.mapGame(game)))),
        add: (id, details) => myGameService.addToLibrary(id, details).pipe(map((entry) => this.mapRequiredLibraryEntry(entry))),
        update: (libId, id, details) => myGameService.updateLibraryEntry(libId, id, details).pipe(map((entry) => this.mapRequiredLibraryEntry(entry))),
      },
      animes: {
        list: (filter) => animeService.getAll(filter).pipe(map((page) => this.mapPage(page, (anime) => this.mapAnime(anime)))),
        add: (id, details) => myAnimeService.addToLibrary(id, details).pipe(map((entry) => this.mapRequiredLibraryEntry(entry))),
        update: (libId, id, details) => myAnimeService.updateLibraryEntry(libId, id, details).pipe(map((entry) => this.mapRequiredLibraryEntry(entry))),
      },
      movies: {
        list: (filter) => movieService.getAll(filter).pipe(map((page) => this.mapPage(page, (movie) => this.mapMovie(movie)))),
        add: (id, details) => myMovieService.addToLibrary(id, details).pipe(map((entry) => this.mapRequiredLibraryEntry(entry))),
        update: (libId, id, details) => myMovieService.updateLibraryEntry(libId, id, details).pipe(map((entry) => this.mapRequiredLibraryEntry(entry))),
      },
      series: {
        list: (filter) => seriesService.getAll(filter).pipe(map((page) => this.mapPage(page, (series) => this.mapSeries(series)))),
        add: (id, details) => mySeriesService.addToLibrary(id, details).pipe(map((entry) => this.mapRequiredLibraryEntry(entry))),
        update: (libId, id, details) => mySeriesService.updateLibraryEntry(libId, id, details).pipe(map((entry) => this.mapRequiredLibraryEntry(entry))),
      },
    };
  }

  getAll(mediaFilter?: MediaFilter): Observable<PaginateResult<MediaItem>> {
    return this.currentStrategy().list(mediaFilter);
  }

  addToLibrary(mediaId: number, details: LibraryEntryDetails): Observable<UserMediaEntry> {
    return this.currentStrategy().add(mediaId, details);
  }

  updateLibraryEntry(libraryEntryId: number, mediaId: number, details: LibraryEntryDetails): Observable<UserMediaEntry> {
    return this.currentStrategy().update(libraryEntryId, mediaId, details);
  }

  detailsRoute(item: MediaItem): unknown[] {
    return ['/library', this.mediaMode.mode().id, item.id];
  }

  private currentStrategy(): MediaStrategy {
    return this.strategies[this.mediaMode.mode().id];
  }

  private mapPage<T>(result: PaginateResult<T>, mapper: (item: T) => MediaItem): PaginateResult<MediaItem> {
    return {
      ...result,
      data: (result.data ?? []).map(mapper),
    } as PaginateResult<MediaItem>;
  }

  private mapGame(game: Game): MediaItem {
    return {
      id: game.id,
      name: game.name,
      description: game.description,
      releaseDate: game.releaseDate,
      genre: game.genre,
      image: game.image,
      kind: 'games',
      libraryEntry: this.mapLibraryEntry(game.myGames),
      platforms: game.platforms,
      playtime: game.playtime,
    };
  }

  private mapAnime(anime: Anime): MediaItem {
    return {
      id: anime.id,
      name: anime.name,
      description: anime.description,
      releaseDate: anime.releaseDate,
      genre: anime.genre,
      image: anime.image,
      kind: 'animes',
      libraryEntry: this.mapLibraryEntry(anime.myAnimes),
      expectedWatchTimePerEpisodeMinutes: anime.expectedWatchTimePerEpisodeMinutes,
      expectedWatchTimeMinutes: anime.expectedWatchTimeMinutes,
      episodeCount: anime.episodeCount,
    };
  }

  private mapMovie(movie: Movie): MediaItem {
    return {
      id: movie.id,
      name: movie.name,
      description: movie.description,
      releaseDate: movie.releaseDate,
      genre: movie.genre,
      image: movie.image,
      kind: 'movies',
      libraryEntry: this.mapLibraryEntry(movie.myMovies),
      expectedWatchTimeMinutes: movie.expectedWatchTimeMinutes,
    };
  }

  private mapSeries(series: Series): MediaItem {
    return {
      id: series.id,
      name: series.name,
      description: series.description,
      releaseDate: series.releaseDate,
      genre: series.genre,
      image: series.image,
      kind: 'series',
      libraryEntry: this.mapLibraryEntry(series.mySeries),
      expectedWatchTimePerEpisodeMinutes: series.expectedWatchTimePerEpisodeMinutes,
      expectedWatchTimeMinutes: series.expectedWatchTimeMinutes,
      episodeCount: series.episodeCount,
    };
  }

  private mapLibraryEntry(entry: Game['myGames'] | MyAnime | MyMovie | MySeries | null | undefined): UserMediaEntry | null {
    if (!entry) {
      return null;
    }

    return {
      id: entry.id,
      rating: entry.rating,
      startDate: entry.startDate,
      endDate: entry.endDate,
      status: entry.status,
      timeSpend: entry.timeSpend,
      personalNotes: 'personalNotes' in entry ? entry.personalNotes as string | null : null,
      myGameInfo: 'myGameInfo' in entry ? entry.myGameInfo : null,
      currentWatchTimeMinutes: 'currentWatchTimeMinutes' in entry ? entry.currentWatchTimeMinutes : null,
      currentEpisode: 'currentEpisode' in entry ? entry.currentEpisode : null,
    };
  }

  private mapRequiredLibraryEntry(entry: Game['myGames'] | MyAnime | MyMovie | MySeries): UserMediaEntry {
    const mapped = this.mapLibraryEntry(entry);
    if (!mapped) {
      throw new Error('Saved library entry was empty.');
    }

    return mapped;
  }
}
