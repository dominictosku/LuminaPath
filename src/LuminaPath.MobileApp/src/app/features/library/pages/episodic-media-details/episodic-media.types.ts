import { Observable } from 'rxjs';
import { MediaFile } from '../../models/mediaFile.model';
import { LibraryEntryDetails } from '../../models/media-item.model';

export type EpisodicMediaKind = 'animes' | 'series';

export interface EpisodicMediaSummary {
  id: number;
  name: string;
  releaseDate?: Date | string | null;
  image?: MediaFile | null;
}

export interface EpisodicLibraryEntry {
  id: number;
  status: number | null;
  rating: number | null;
  startDate: Date | string | null;
  endDate: Date | string | null;
  timeSpend: number | null;
  currentEpisode: number | null;
}

export interface EpisodicMediaView {
  id: number;
  name: string;
  description: string;
  releaseDate: Date | string | null;
  genre: string;
  expectedWatchTimePerEpisodeMinutes: number | null;
  expectedWatchTimeMinutes: number | null;
  episodeCount: number | null;
  parentId: number | null;
  parentName: string | null;
  seasons: EpisodicMediaSummary[];
  image: MediaFile | null;
  libraryEntry: EpisodicLibraryEntry | null;
}

/**
 * Catalog-edit payload built by the details page from the create-dialog
 * form. The provider-supplied adapter assembles the entity-specific
 * request (Anime / Series) from it before calling its typed service's
 * PUT — that's the only place that knows which navigation collections
 * to scrub so EF doesn't try to upsert personal user-library rows
 * (see the Game edit bug fix for the same lesson).
 */
export interface CatalogPatch {
  name: string;
  description: string;
  releaseDate: string;
  genre: string;
  episodeCount: number | null;
  expectedWatchTimePerEpisodeMinutes: number | null;
  expectedWatchTimeMinutes: number | null;
  image: MediaFile | null;
}

export interface EpisodicMediaAdapter {
  load(id: number): Observable<EpisodicMediaView>;
  add(mediaId: number, details: LibraryEntryDetails): Observable<unknown>;
  update(libraryEntryId: number, mediaId: number, details: LibraryEntryDetails): Observable<unknown>;
  delete(libraryEntryId: number): Observable<unknown>;
  /** Admin / editor: PUT the catalog entry with a fresh payload (no personal collections). */
  updateCatalog(id: number, patch: CatalogPatch, existing: EpisodicMediaView): Observable<unknown>;
  /** Admin / editor: DELETE the catalog entry. */
  deleteCatalog(id: number): Observable<unknown>;
}

export type FourthMetaSource = 'expectedWatchTimeMinutes' | 'expectedWatchTimePerEpisodeMinutes';

export interface EpisodicMediaConfig {
  kind: EpisodicMediaKind;
  paramKey: string;
  notFoundLabel: string;
  loadErrorLabel: string;
  loadingLabel: string;
  titleFallback: string;
  eyebrowLabel: string;
  removeMessage: string;
  parentLabelFallback: string;
  optionsHeaderFallback: string;
  replayLabel: string;
  errorIcon: string;
  progressEyebrowIcon: string;
  seasonRouteBase: string;
  fourthMetaLabel: string;
  fourthMetaSource: FourthMetaSource;
  perEpisodeSuffix?: string;
}
