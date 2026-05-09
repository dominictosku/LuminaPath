import { MediaFile } from './mediaFile.model';

export type MediaKind = 'games' | 'animes' | 'movies';

export interface UserMediaEntry {
  id: number;
  rating: number | null;
  startDate: Date | string | null;
  endDate: Date | string | null;
  status: number;
  timeSpend: number | null;
  myGameInfo?: { trackedHours?: number | null } | null;
  currentWatchTimeMinutes?: number | null;
  currentEpisode?: number | null;
}

export interface MediaItem {
  id: number;
  name: string;
  description: string;
  releaseDate: Date | string | null;
  genre: string;
  image: MediaFile | null;
  kind: MediaKind;
  libraryEntry: UserMediaEntry | null;
  platforms?: number | null;
  playtime?: number | null;
  expectedWatchTimeMinutes?: number | null;
  episodeCount?: number | null;
}

export type LibraryEntryDetails = {
  status: number;
  timeSpend: number | null;
  rating?: number | null;
  startDate?: string | null;
  endDate?: string | null;
  currentEpisode?: number | null;
};
