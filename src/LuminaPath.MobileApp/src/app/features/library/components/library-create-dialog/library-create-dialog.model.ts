import { LibraryEntryDetails, MediaKind } from '../../models/media-item.model';
import { MediaFile } from '../../models/mediaFile.model';

/**
 * Draft state the create dialog binds to. The page maps this to the typed
 * Game / Anime / Series payload before calling its respective service.
 */
export interface CreateMediaForm {
  name: string;
  description: string;
  releaseDate: string;
  genre: string;
  /** Games — bitmask of selected platforms. */
  platforms: number;
  /** Games — estimated playtime in hours. */
  playtime: number | null;
  /** Animes / Series — total expected runtime in minutes. */
  expectedWatchTimeMinutes: number | null;
  /** Animes / Series — per-episode runtime in minutes. */
  expectedWatchTimePerEpisodeMinutes: number | null;
  /** Animes / Series — total episode count. */
  episodeCount: number | null;
  /** When true, also create a personal library entry on submit. */
  createLibraryEntry: boolean;
  /** The personal library draft — mirrors LibraryEntryDetails plus episode. */
  libraryEntry: LibraryEntryDetails;
  /**
   * Cover image, populated after a successful upload to `/api/files`.
   * Sent to the backend on submit as the entity's `image` field.
   */
  cover: MediaFile | null;
  /**
   * Local object URL for the picked file — used to render an immediate
   * preview before the upload completes. Cleared when the form resets.
   */
  coverPreviewUrl: string | null;
}

export function emptyCreateForm(): CreateMediaForm {
  return {
    name: '',
    description: '',
    releaseDate: '',
    genre: '',
    platforms: 0,
    playtime: null,
    expectedWatchTimeMinutes: null,
    expectedWatchTimePerEpisodeMinutes: null,
    episodeCount: null,
    createLibraryEntry: false,
    libraryEntry: {
      status: 1,
      timeSpend: 0,
      rating: null,
      startDate: null,
      endDate: null,
      personalNotes: null,
      currentEpisode: null,
    },
    cover: null,
    coverPreviewUrl: null,
  };
}

/**
 * Outbound payload emitted by the dialog on submit. The page picks the
 * right service per kind and POSTs the catalog item, then (optionally)
 * the matching personal entry.
 */
export interface CreateMediaSubmission {
  kind: MediaKind;
  form: CreateMediaForm;
}
