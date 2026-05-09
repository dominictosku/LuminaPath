import { type IBasicInfo } from 'src/app/core/interfaces/iBasicInfo';
import { MediaFile } from '../../media/models/mediaFile.model';

export class Movie implements IBasicInfo {
  id = 0;
  name = '';
  description = '';
  releaseDate: Date | string | null = null;
  genre = '';
  expectedWatchTimeMinutes: number | null = null;
  myMovies: MyMovie | null = null;
  image: MediaFile | null = null;
}

export class MyMovie implements IBasicInfo {
  id = 0;
  rating: number | null = null;
  startDate: Date | string | null = null;
  endDate: Date | string | null = null;
  status = 1;
  timeSpend: number | null = null;
  movieId: number;
  movie: Movie | null = null;
  currentWatchTimeMinutes: number | null = null;

  constructor(movieId = 0) {
    this.movieId = movieId;
  }
}
