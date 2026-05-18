import { Game } from 'src/app/features/games/models/games.model';

/**
 * The library entry shape used by detail-page getters.
 * Looser than `MyGame` from the games model to tolerate API variation
 * (e.g. `myGames` arriving as a single object OR a 1-element array).
 */
export type GameLibraryEntry = {
  id?: number;
  status?: number;
  timeSpend?: number | null;
  rating?: number | null;
  startDate?: Date | string | null;
  endDate?: Date | string | null;
  personalNotes?: string | null;
};

export type GameWithFlexibleLibrary = Game & {
  myGames?: GameLibraryEntry | GameLibraryEntry[] | null;
};

export type UserGameAchievement = {
  id: number;
  gameAchievementId: number;
  provider: number;
  providerName: string;
  sourceAchievementId: string;
  title: string;
  description?: string | null;
  iconUrl?: string | null;
  isHidden: boolean;
  trophyType?: string | null;
  unlockedAt?: string | null;
  syncedAt: string;
};
