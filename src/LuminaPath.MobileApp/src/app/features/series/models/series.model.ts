import { type IBasicInfo } from 'src/app/core/interfaces/iBasicInfo';
import { MediaFile } from '../../library/models/mediaFile.model';

export class Series implements IBasicInfo {
  id = 0;
  name = '';
  description = '';
  releaseDate: Date | string | null = null;
  genre = '';
  expectedWatchTimePerEpisodeMinutes: number | null = null;
  expectedWatchTimeMinutes: number | null = null;
  episodeCount: number | null = null;
  mySeries: MySeries | null = null;
  image: MediaFile | null = null;
}

export class MySeries implements IBasicInfo {
  id = 0;
  rating: number | null = null;
  startDate: Date | string | null = null;
  endDate: Date | string | null = null;
  status = 1;
  timeSpend: number | null = null;
  seriesId: number;
  series: Series | null = null;
  currentWatchTimeMinutes: number | null = null;
  currentEpisode: number | null = null;

  constructor(seriesId = 0) {
    this.seriesId = seriesId;
  }
}
