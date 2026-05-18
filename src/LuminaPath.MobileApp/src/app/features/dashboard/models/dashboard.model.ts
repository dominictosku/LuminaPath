import { MediaFile } from 'src/app/features/library/models/mediaFile.model';

/** Tone tokens used by both metric tiles and focus chips for color theming. */
export type DashboardTone = 'blue' | 'green' | 'amber' | 'rose';

/** Display-cased media kind used in dashboard items.
 *  (Distinct from the lowercase-plural `MediaKind` in `media-item.model.ts`,
 *  which is the API/store-level discriminator.) */
export type DashboardMediaKind = 'Game' | 'Anime' | 'Movie' | 'Series';

export type DashboardMetric = {
  label: string;
  value: string;
  detail: string;
  icon: string;
  tone: DashboardTone;
};

export type DashboardMediaItem = {
  id: number;
  kind: DashboardMediaKind;
  name: string;
  description?: string | null;
  genre?: string | null;
  releaseDate: Date | string | null;
  image: MediaFile | null;
  status: number;
  estimatedHours: number;
  playedHours: number;
  remainingHours: number;
  context: string;
};

export type DashboardFocusItem = {
  title: string;
  detail: string;
  icon: string;
  tone: DashboardTone;
};

export type DashboardActivityItem = {
  title: string;
  detail: string;
  icon: string;
};
