import { GameLibraryEntry } from 'src/app/features/my-games/models/my-game.model';
import { serializeDate } from 'src/app/shared/utils/date-helpers';

export type LibraryUpdateOverrides = Partial<{
  status: number;
  timeSpend: number | null;
  rating: number | null;
  startDate: string | null;
  endDate: string | null;
  personalNotes: string | null;
}>;

/** Build the library-entry update payload, defaulting from the current entry. */
export function buildLibraryUpdate(entry: GameLibraryEntry | null, overrides: LibraryUpdateOverrides = {}) {
  const current = entry ?? {};
  return {
    status: Number(current.status ?? 1),
    timeSpend: current.timeSpend ?? null,
    rating: current.rating ?? null,
    startDate: serializeDate(current.startDate),
    endDate: serializeDate(current.endDate),
    personalNotes: current.personalNotes ?? null,
    ...overrides,
  };
}

/** A fresh session bumps a not-started (status 1) entry to in-progress (2). */
export function nextLiveSessionStatus(entry: GameLibraryEntry | null): number {
  const status = Number(entry?.status ?? 1);
  return status === 1 ? 2 : status;
}

/** Add a session's minutes to the entry's tracked hours, rounded to 2dp. */
export function addTrackedHours(entry: GameLibraryEntry | null, durationMinutes: number): number {
  const current = Number(entry?.timeSpend ?? 0);
  return Math.round((current + durationMinutes / 60) * 100) / 100;
}

export function trackedHoursLabel(entry: GameLibraryEntry | null): string {
  const hours = Number(entry?.timeSpend ?? 0);
  return hours > 0 ? `${Math.round(hours * 10) / 10}h tracked` : '';
}
