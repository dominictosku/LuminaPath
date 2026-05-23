import { Provider } from '@angular/core';
import { map } from 'rxjs';
import {
  EPISODIC_MEDIA_ADAPTER,
  EPISODIC_MEDIA_CONFIG,
} from '../../library/pages/episodic-media-details/episodic-media.tokens';
import {
  EpisodicMediaAdapter,
  EpisodicMediaConfig,
  EpisodicMediaView,
} from '../../library/pages/episodic-media-details/episodic-media.types';
import { Anime, AnimeSummary } from '../models/animes.model';
import { AnimeService } from '../services/anime.service';
import { MyAnimeService } from '../services/my-anime.service';

const ANIME_DETAILS_CONFIG: EpisodicMediaConfig = {
  kind: 'animes',
  paramKey: 'animeId',
  notFoundLabel: 'Anime not found.',
  loadErrorLabel: 'Anime could not be loaded.',
  loadingLabel: 'Loading anime details...',
  titleFallback: 'Anime details',
  eyebrowLabel: 'Anime details',
  removeMessage: 'Your watch progress and dates for this anime will be permanently deleted.',
  parentLabelFallback: 'parent anime',
  optionsHeaderFallback: 'Anime options',
  replayLabel: 'Replay',
  errorIcon: 'sparkles-outline',
  progressEyebrowIcon: 'sparkles-outline',
  seasonRouteBase: '/library/animes',
  fourthMetaLabel: 'Watch time',
  fourthMetaSource: 'expectedWatchTimeMinutes',
};

function toView(anime: Anime): EpisodicMediaView {
  return {
    id: anime.id,
    name: anime.name,
    description: anime.description,
    releaseDate: anime.releaseDate,
    genre: anime.genre,
    expectedWatchTimePerEpisodeMinutes: anime.expectedWatchTimePerEpisodeMinutes,
    expectedWatchTimeMinutes: anime.expectedWatchTimeMinutes,
    episodeCount: anime.episodeCount,
    parentId: anime.parentAnimeId,
    parentName: anime.parentAnimeName,
    seasons: (anime.seasons ?? []).map(toSeasonSummary),
    image: anime.image,
    libraryEntry: anime.myAnimes
      ? {
          id: anime.myAnimes.id,
          status: anime.myAnimes.status,
          rating: anime.myAnimes.rating,
          startDate: anime.myAnimes.startDate,
          endDate: anime.myAnimes.endDate,
          timeSpend: anime.myAnimes.timeSpend,
          currentEpisode: anime.myAnimes.currentEpisode,
        }
      : null,
  };
}

function toSeasonSummary(season: AnimeSummary) {
  return {
    id: season.id,
    name: season.name,
    releaseDate: season.releaseDate ?? null,
    image: season.image ?? null,
  };
}

function createAdapter(animeService: AnimeService, myAnimeService: MyAnimeService): EpisodicMediaAdapter {
  return {
    load: (id) => animeService.get(id).pipe(map(toView)),
    add: (mediaId, details) => myAnimeService.addToLibrary(mediaId, details),
    update: (libId, mediaId, details) => myAnimeService.updateLibraryEntry(libId, mediaId, details),
    delete: (libId) => myAnimeService.delete(libId),
    updateCatalog: (id, patch, existing) => {
      // Build from scratch — never spread the loaded view. Echoing back
      // `myAnimes` / `seasons` makes EF try to upsert personal entries
      // and trips FK_MyAnimes_AspNetUsers_LuminaUserId (same lesson as
      // the Game edit fix).
      const anime = Object.assign(new Anime(), {
        id,
        name: patch.name,
        description: patch.description,
        releaseDate: patch.releaseDate || existing.releaseDate || null,
        genre: patch.genre,
        episodeCount: patch.episodeCount,
        expectedWatchTimePerEpisodeMinutes: patch.expectedWatchTimePerEpisodeMinutes,
        expectedWatchTimeMinutes: patch.expectedWatchTimeMinutes,
        image: patch.image ?? existing.image,
        parentAnimeId: existing.parentId,
        parentAnimeName: existing.parentName,
        myAnimes: null,
        seasons: null,
      });
      return animeService.put(id, anime);
    },
    deleteCatalog: (id) => animeService.delete(id),
  };
}

export const animeDetailsProviders: Provider[] = [
  { provide: EPISODIC_MEDIA_CONFIG, useValue: ANIME_DETAILS_CONFIG },
  {
    provide: EPISODIC_MEDIA_ADAPTER,
    useFactory: createAdapter,
    deps: [AnimeService, MyAnimeService],
  },
];
