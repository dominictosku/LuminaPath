import { type IBasicInfo } from 'src/app/core/entities/iBasicInfo';
import { MediaFile } from '../../library/models/mediaFile.model';

export class Anime implements IBasicInfo {
  id = 0;
  name = '';
  description = '';
  releaseDate: Date | string | null = null;
  genre = '';
  expectedWatchTimePerEpisodeMinutes: number | null = null;
  expectedWatchTimeMinutes: number | null = null;
  episodeCount: number | null = null;
  parentAnimeId: number | null = null;
  parentAnimeName: string | null = null;
  seasons: AnimeSummary[] | null = null;
  myAnimes: MyAnime | null = null;
  image: MediaFile | null = null;
}

export interface AnimeSummary {
  id: number;
  name: string;
  description?: string | null;
  releaseDate?: Date | string | null;
  episodeCount?: number | null;
  expectedWatchTimePerEpisodeMinutes?: number | null;
  expectedWatchTimeMinutes?: number | null;
  image?: MediaFile | null;
  parentAnimeId?: number | null;
}

export class MyAnime implements IBasicInfo {
  id = 0;
  rating: number | null = null;
  startDate: Date | string | null = null;
  endDate: Date | string | null = null;
  status = 1;
  timeSpend: number | null = null;
  animeId: number;
  anime: Anime | null = null;
  currentWatchTimeMinutes: number | null = null;
  currentEpisode: number | null = null;

  constructor(animeId = 0) {
    this.animeId = animeId;
  }
}
