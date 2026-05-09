import { type IBasicInfo } from 'src/app/core/interfaces/iBasicInfo';
import { MediaFile } from '../../media/models/mediaFile.model';

export class Anime implements IBasicInfo {
  id = 0;
  name = '';
  description = '';
  releaseDate: Date | string | null = null;
  genre = '';
  expectedWatchTimeMinutes: number | null = null;
  episodeCount: number | null = null;
  myAnimes: MyAnime | null = null;
  image: MediaFile | null = null;
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
