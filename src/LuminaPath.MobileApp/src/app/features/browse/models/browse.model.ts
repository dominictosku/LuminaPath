import { MediaFile } from '../../library/models/mediaFile.model';
import { UserMediaEntry } from '../../library/models/media-item.model';

export type BrowseKind = 'games' | 'animes';

export interface BrowseItem {
  id: number;
  name: string;
  description: string | null;
  genre: string | null;
  releaseDate: Date | string | null;
  kind: BrowseKind;
  addedCount: number;
  image: MediaFile | null;
  platforms?: number | null;
  playtime?: number | null;
  episodeCount?: number | null;
  expectedWatchTimePerEpisodeMinutes?: number | null;
  expectedWatchTimeMinutes?: number | null;
  libraryEntry?: UserMediaEntry | null;
}

export interface BrowseGroup {
  id: string;
  title: string;
  subtitle: string;
  sortValue: number;
  items: BrowseItem[];
}
