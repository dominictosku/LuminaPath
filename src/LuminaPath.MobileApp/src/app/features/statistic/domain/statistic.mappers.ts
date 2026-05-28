import { Anime } from '../../animes/models/animes.model';
import { Game } from '../../games/models/games.model';
import { Movie } from '../../movies/models/movies.model';
import { Series } from '../../series/models/series.model';
import { BacklogItem, StatisticKind } from '../models/statistic.model';

/**
 * Convert raw API entities to the normalized {@link BacklogItem} shape used by
 * every chart on the page. Status codes differ between games and watch-media:
 * games complete on 4 ("Completed") / 5 ("Main game"), watch-media completes
 * on 3 ("Completed") and uses 4 for "Dropped". Both share 2 for the active
 * status ("Playing" / "Watching").
 */

export function fromGame(game: Game): BacklogItem {
  const status = Number(game.myGames?.status ?? -1);
  const estimatedHours = Number(game.playtime) || 0;
  const consumedHours = (Number(game.myGames?.timeSpend) || 0) + (Number(game.myGames?.myGameInfo?.trackedHours) || 0);
  return createItem({
    id: game.id,
    kind: 'Games',
    name: game.name,
    status,
    estimatedHours,
    consumedHours,
    completedStatus: 4,
    activeStatus: 2,
    droppedStatus: null,
    statusLabel: gameStatusLabel(status),
    rating: game.myGames?.rating ?? null,
    genre: game.genre ?? '',
    startDate: toDate(game.myGames?.startDate),
    endDate: toDate(game.myGames?.endDate),
  });
}

export function fromAnime(anime: Anime): BacklogItem {
  return fromWatchItem(
    'Anime',
    anime.id,
    anime.name,
    anime.myAnimes?.status,
    anime.expectedWatchTimeMinutes,
    anime.myAnimes?.currentWatchTimeMinutes,
    anime.myAnimes?.rating ?? null,
    anime.genre ?? '',
    toDate(anime.myAnimes?.startDate),
    toDate(anime.myAnimes?.endDate),
  );
}

export function fromMovie(movie: Movie): BacklogItem {
  return fromWatchItem(
    'Movies',
    movie.id,
    movie.name,
    movie.myMovies?.status,
    movie.expectedWatchTimeMinutes,
    movie.myMovies?.currentWatchTimeMinutes,
    movie.myMovies?.rating ?? null,
    movie.genre ?? '',
    toDate(movie.myMovies?.startDate),
    toDate(movie.myMovies?.endDate),
  );
}

export function fromSeries(series: Series): BacklogItem {
  return fromWatchItem(
    'Series',
    series.id,
    series.name,
    series.mySeries?.status,
    series.expectedWatchTimeMinutes,
    series.mySeries?.currentWatchTimeMinutes,
    series.mySeries?.rating ?? null,
    series.genre ?? '',
    toDate(series.mySeries?.startDate),
    toDate(series.mySeries?.endDate),
  );
}

function fromWatchItem(
  kind: StatisticKind,
  id: number,
  name: string,
  statusValue: number | null | undefined,
  expectedMinutes: number | null | undefined,
  watchedMinutes: number | null | undefined,
  rating: number | null,
  genre: string,
  startDate: Date | null,
  endDate: Date | null,
): BacklogItem {
  const status = Number(statusValue ?? -1);
  return createItem({
    id,
    kind,
    name,
    status,
    estimatedHours: (Number(expectedMinutes) || 0) / 60,
    consumedHours: (Number(watchedMinutes) || 0) / 60,
    completedStatus: 3,
    activeStatus: 2,
    droppedStatus: 4,
    statusLabel: watchStatusLabel(status),
    rating,
    genre,
    startDate,
    endDate,
  });
}

function createItem(input: {
  id: number;
  kind: StatisticKind;
  name: string;
  status: number;
  estimatedHours: number;
  consumedHours: number;
  completedStatus: number;
  activeStatus: number;
  droppedStatus: number | null;
  statusLabel: string;
  rating: number | null;
  genre: string;
  startDate: Date | null;
  endDate: Date | null;
}): BacklogItem {
  return {
    id: input.id,
    kind: input.kind,
    name: input.name,
    status: input.status,
    statusLabel: input.statusLabel,
    estimatedHours: input.estimatedHours,
    consumedHours: input.consumedHours,
    remainingHours: Math.max(0, input.estimatedHours - input.consumedHours),
    completed: input.status === input.completedStatus,
    active: input.status === input.activeStatus,
    dropped: input.droppedStatus != null && input.status === input.droppedStatus,
    rating: input.rating != null ? Number(input.rating) : null,
    genre: input.genre ?? '',
    startDate: input.startDate,
    endDate: input.endDate,
  };
}

function toDate(value: Date | string | null | undefined): Date | null {
  if (!value) {
    return null;
  }
  const date = value instanceof Date ? value : new Date(value);
  return isNaN(date.getTime()) ? null : date;
}

function gameStatusLabel(status: number): string {
  return {
    0: 'On hold',
    1: 'Planned',
    2: 'Playing',
    3: 'Story complete',
    4: 'Completed',
    5: 'Main game',
  }[status] ?? 'Catalog';
}

function watchStatusLabel(status: number): string {
  return {
    0: 'On hold',
    1: 'Planned',
    2: 'Watching',
    3: 'Completed',
    4: 'Dropped',
  }[status] ?? 'Catalog';
}
