import { Game } from '../models/games.model';
import { GameStatus } from '../../library/models/library-status.model';

export function gameStatusOf(game: Game): number {
  return Number(game.myGames?.status ?? -1);
}

export function playedHoursOfGame(game: Game): number {
  const manual = Number(game.myGames?.timeSpend) || 0;
  const tracked = Number(game.myGames?.myGameInfo?.trackedHours) || 0;
  return manual + tracked;
}

export function estimatedHoursOfGame(game: Game): number {
  return Number(game.playtime) || 0;
}

export function remainingHoursOfGame(game: Game): number {
  return Math.max(0, estimatedHoursOfGame(game) - playedHoursOfGame(game));
}

export function progressRatioOfGame(game: Game): number {
  const estimated = estimatedHoursOfGame(game);

  if (estimated <= 0) {
    return gameStatusOf(game) === GameStatus.Completed ? 1 : 0;
  }

  return Math.min(1, playedHoursOfGame(game) / estimated);
}

export function releaseDateOfGame(game: { releaseDate?: Date | string | null }): Date {
  return game.releaseDate ? new Date(game.releaseDate) : new Date(Number.NaN);
}
