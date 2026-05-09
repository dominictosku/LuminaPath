import { MediaFile } from '../../media/models/mediaFile.model';
import { UserMediaEntry } from '../../media/models/media-item.model';

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
  expectedWatchTimePerEpisodeMinutes?: number | null;
  expectedWatchTimeMinutes?: number | null;
  libraryEntry?: UserMediaEntry | null;
}

export interface ReleaseLibraryGroup {
  id: string;
  title: string;
  subtitle: string;
  sortValue: number;
  items: ReleaseLibraryItem[];
}
