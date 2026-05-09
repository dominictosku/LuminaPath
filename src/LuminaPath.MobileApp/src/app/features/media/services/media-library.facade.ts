import { Injectable } from '@angular/core';
import { Observable, map } from 'rxjs';
import { PaginateResult } from 'src/app/core/entities/paginatedResult';
import { MediaFilter } from 'src/app/core/entities/mediaFilter';
import { MediaModeService } from 'src/app/shared/services/media-mode.service';
import { Game } from '../../games/models/games.model';
import { GameService } from '../../games/services/game.service';
import { MyGameService } from '../../my-games/services/my-game.service';
import { Anime, MyAnime } from '../../animes/models/animes.model';
import { AnimeService } from '../../animes/services/anime.service';
import { MyAnimeService } from '../../animes/services/my-anime.service';
import { Movie, MyMovie } from '../../movies/models/movies.model';
import { MovieService } from '../../movies/services/movie.service';
import { MyMovieService } from '../../movies/services/my-movie.service';
import { LibraryEntryDetails, MediaItem, UserMediaEntry } from '../models/media-item.model';

@Injectable({
  providedIn: 'root',
})
export class MediaLibraryFacade {
  constructor(
    private readonly mediaMode: MediaModeService,
    private readonly gameService: GameService,
    private readonly myGameService: MyGameService,
    private readonly animeService: AnimeService,
    private readonly myAnimeService: MyAnimeService,
    private readonly movieService: MovieService,
    private readonly myMovieService: MyMovieService,
  ) {
  }

  getAll(mediaFilter?: MediaFilter): Observable<PaginateResult<MediaItem>> {
    switch (this.mediaMode.current.id) {
      case 'animes':
        return this.animeService.getAll(mediaFilter).pipe(map((result) => this.mapPage(result, (anime) => this.mapAnime(anime))));
      case 'movies':
        return this.movieService.getAll(mediaFilter).pipe(map((result) => this.mapPage(result, (movie) => this.mapMovie(movie))));
      case 'games':
      default:
        return this.gameService.getAll(mediaFilter).pipe(map((result) => this.mapPage(result, (game) => this.mapGame(game))));
    }
  }

  addToLibrary(mediaId: number, details: LibraryEntryDetails): Observable<UserMediaEntry> {
    switch (this.mediaMode.current.id) {
      case 'animes':
        return this.myAnimeService.addToLibrary(mediaId, details).pipe(map((entry) => this.mapRequiredLibraryEntry(entry)));
      case 'movies':
        return this.myMovieService.addToLibrary(mediaId, details).pipe(map((entry) => this.mapRequiredLibraryEntry(entry)));
      case 'games':
      default:
        return this.myGameService.addToLibrary(mediaId, details).pipe(map((entry) => this.mapRequiredLibraryEntry(entry)));
    }
  }

  updateLibraryEntry(libraryEntryId: number, mediaId: number, details: LibraryEntryDetails): Observable<UserMediaEntry> {
    switch (this.mediaMode.current.id) {
      case 'animes':
        return this.myAnimeService.updateLibraryEntry(libraryEntryId, mediaId, details).pipe(map((entry) => this.mapRequiredLibraryEntry(entry)));
      case 'movies':
        return this.myMovieService.updateLibraryEntry(libraryEntryId, mediaId, details).pipe(map((entry) => this.mapRequiredLibraryEntry(entry)));
      case 'games':
      default:
        return this.myGameService.updateLibraryEntry(libraryEntryId, mediaId, details).pipe(map((entry) => this.mapRequiredLibraryEntry(entry)));
    }
  }

  detailsRoute(item: MediaItem): unknown[] {
    const mode = this.mediaMode.current.id;
    return ['/media', mode, item.id];
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

  private mapLibraryEntry(entry: Game['myGames'] | MyAnime | MyMovie | null | undefined): UserMediaEntry | null {
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
      myGameInfo: 'myGameInfo' in entry ? entry.myGameInfo : null,
      currentWatchTimeMinutes: 'currentWatchTimeMinutes' in entry ? entry.currentWatchTimeMinutes : null,
      currentEpisode: 'currentEpisode' in entry ? entry.currentEpisode : null,
    };
  }

  private mapRequiredLibraryEntry(entry: Game['myGames'] | MyAnime | MyMovie): UserMediaEntry {
    const mapped = this.mapLibraryEntry(entry);
    if (!mapped) {
      throw new Error('Saved library entry was empty.');
    }

    return mapped;
  }
}
