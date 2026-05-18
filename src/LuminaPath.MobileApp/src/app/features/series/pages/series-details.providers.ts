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
import { Series, SeriesSummary } from '../models/series.model';
import { SeriesService } from '../services/series.service';
import { MySeriesService } from '../services/my-series.service';

const SERIES_DETAILS_CONFIG: EpisodicMediaConfig = {
  kind: 'series',
  paramKey: 'seriesId',
  notFoundLabel: 'Series not found.',
  loadErrorLabel: 'Series could not be loaded.',
  loadingLabel: 'Loading series details...',
  titleFallback: 'Series details',
  eyebrowLabel: 'Series details',
  removeMessage: 'Your watch progress and dates for this series will be permanently deleted.',
  parentLabelFallback: 'parent series',
  optionsHeaderFallback: 'Series options',
  replayLabel: 'Rewatch',
  errorIcon: 'tv-outline',
  progressEyebrowIcon: 'time-outline',
  seasonRouteBase: '/library/series',
  fourthMetaLabel: 'Per episode',
  fourthMetaSource: 'expectedWatchTimePerEpisodeMinutes',
  perEpisodeSuffix: '/ ep',
};

function toView(series: Series): EpisodicMediaView {
  return {
    id: series.id,
    name: series.name,
    description: series.description,
    releaseDate: series.releaseDate,
    genre: series.genre,
    expectedWatchTimePerEpisodeMinutes: series.expectedWatchTimePerEpisodeMinutes,
    expectedWatchTimeMinutes: series.expectedWatchTimeMinutes,
    episodeCount: series.episodeCount,
    parentId: series.parentSeriesId,
    parentName: series.parentSeriesName,
    seasons: (series.seasons ?? []).map(toSeasonSummary),
    image: series.image,
    libraryEntry: series.mySeries
      ? {
          id: series.mySeries.id,
          status: series.mySeries.status,
          rating: series.mySeries.rating,
          startDate: series.mySeries.startDate,
          endDate: series.mySeries.endDate,
          timeSpend: series.mySeries.timeSpend,
          currentEpisode: series.mySeries.currentEpisode,
        }
      : null,
  };
}

function toSeasonSummary(season: SeriesSummary) {
  return {
    id: season.id,
    name: season.name,
    releaseDate: season.releaseDate ?? null,
    image: season.image ?? null,
  };
}

function createAdapter(seriesService: SeriesService, mySeriesService: MySeriesService): EpisodicMediaAdapter {
  return {
    load: (id) => seriesService.get(id).pipe(map(toView)),
    add: (mediaId, details) => mySeriesService.addToLibrary(mediaId, details),
    update: (libId, mediaId, details) => mySeriesService.updateLibraryEntry(libId, mediaId, details),
    delete: (libId) => mySeriesService.delete(libId),
  };
}

export const seriesDetailsProviders: Provider[] = [
  { provide: EPISODIC_MEDIA_CONFIG, useValue: SERIES_DETAILS_CONFIG },
  {
    provide: EPISODIC_MEDIA_ADAPTER,
    useFactory: createAdapter,
    deps: [SeriesService, MySeriesService],
  },
];
