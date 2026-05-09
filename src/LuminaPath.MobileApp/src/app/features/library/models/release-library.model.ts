import { MediaFile } from '../../media/models/mediaFile.model';

export type ReleaseLibraryKind = 'games' | 'animes';

export interface ReleaseLibraryItem {
  id: number;
  name: string;
  description: string | null;
  genre: string | null;
  releaseDate: Date | string | null;
  kind: ReleaseLibraryKind;
  addedCount: number;
  image: MediaFile | null;
  platforms?: number | null;
  playtime?: number | null;
  episodeCount?: number | null;
  expectedWatchTimeMinutes?: number | null;
}

export interface ReleaseLibraryGroup {
  id: string;
  title: string;
  subtitle: string;
  sortValue: number;
  items: ReleaseLibraryItem[];
}
