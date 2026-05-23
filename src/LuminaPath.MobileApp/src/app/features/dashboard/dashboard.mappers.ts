import { Anime } from '../animes/models/animes.model';
import { Game, platformLabelFromValue } from '../games/models/games.model';
import { Movie } from '../movies/models/movies.model';
import { Series } from '../series/models/series.model';
import {
  estimatedHoursOfGame,
  gameStatusOf,
  playedHoursOfGame,
  remainingHoursOfGame,
} from '../games/domain/game-library-metrics';

import { DashboardMediaItem, DashboardMediaKind } from './models/dashboard.model';

/**
 * Normalize the four raw media API shapes (Game / Anime / Movie / Series)
 * into the single {@link DashboardMediaItem} the dashboard page works with.
 *
 * Games carry rich `playtime` + `myGames.timeSpend` + `trackedHours` fields;
 * watch-media carry `expectedWatchTimeMinutes` + `currentWatchTimeMinutes`.
 * We collapse both onto the same `estimatedHours` / `playedHours` /
 * `remainingHours` numbers so downstream derivations don't need to care.
 */

export function fromGame(game: Game): DashboardMediaItem {
  return {
    id: game.id,
    kind: 'Game',
    name: game.name,
    description: game.description,
    genre: game.genre,
    releaseDate: game.releaseDate,
    image: game.image,
    status: gameStatusOf(game),
    estimatedHours: estimatedHoursOfGame(game),
    playedHours: playedHoursOfGame(game),
    remainingHours: remainingHoursOfGame(game),
    context: platformLabelFromValue(game.platforms),
  };
}

export function fromAnime(anime: Anime): DashboardMediaItem {
  return fromWatchMedia(
    'Anime',
    anime,
    anime.myAnimes?.status,
    anime.expectedWatchTimeMinutes,
    anime.myAnimes?.currentWatchTimeMinutes,
  );
}

export function fromMovie(movie: Movie): DashboardMediaItem {
  return fromWatchMedia(
    'Movie',
    movie,
    movie.myMovies?.status,
    movie.expectedWatchTimeMinutes,
    movie.myMovies?.currentWatchTimeMinutes,
  );
}

export function fromSeries(series: Series): DashboardMediaItem {
  return fromWatchMedia(
    'Series',
    series,
    series.mySeries?.status,
    series.expectedWatchTimeMinutes,
    series.mySeries?.currentWatchTimeMinutes,
  );
}

function fromWatchMedia(
  kind: DashboardMediaKind,
  media: Anime | Movie | Series,
  status: number | null | undefined,
  expectedMinutes: number | null | undefined,
  watchedMinutes: number | null | undefined,
): DashboardMediaItem {
  const estimatedHours = Math.round(((Number(expectedMinutes) || 0) / 60) * 10) / 10;
  const playedHours = Math.round(((Number(watchedMinutes) || 0) / 60) * 10) / 10;

  return {
    id: media.id,
    kind,
    name: media.name,
    description: media.description,
    genre: media.genre,
    releaseDate: media.releaseDate,
    image: media.image,
    status: Number(status ?? -1),
    estimatedHours,
    playedHours,
    remainingHours: Math.max(0, estimatedHours - playedHours),
    context: kind,
  };
}
