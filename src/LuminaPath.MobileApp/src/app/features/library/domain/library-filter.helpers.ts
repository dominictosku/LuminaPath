import { MediaFilter } from 'src/app/core/entities/mediaFilter';
import { addDays, startOfDay, toISODate } from 'src/app/shared/utils/date-helpers';

import {
  OwnershipFilter,
  ReleaseDateFilter,
  SmartFilter,
  SortMode,
} from '../models/library-filter.model';

export type LibraryFilterState = {
  searchTerm: string;
  ownershipFilter: OwnershipFilter;
  statusFilter: string; // 'all' | numeric status code as string
  platformFilter: string; // 'all' | numeric platform id as string
  releaseDateFilter: ReleaseDateFilter;
  releaseDateFrom: string; // 'YYYY-MM-DD' input value
  releaseDateTo: string;
  sortMode: SortMode;
  smartFilter: SmartFilter;
};

export type DateRange = { from: string | null; to: string | null };

/**
 * Resolve the user-facing release-date filter preset into the absolute
 * `from`/`to` window the backend expects. `custom` reads the page's two
 * `releaseDateFrom`/`releaseDateTo` inputs and adds a day to `to` so the
 * filter is inclusive of the chosen end date.
 *
 * Pulled out of LibraryPage so the date math can be unit-tested without
 * spinning up Angular.
 */
export function releaseDateRange(state: LibraryFilterState, now: Date = new Date()): DateRange {
  const year = now.getFullYear();

  switch (state.releaseDateFilter) {
    case 'released':
      return { from: null, to: toISODate(addDays(now, 1)) };
    case 'upcoming':
      return { from: toISODate(startOfDay(now)), to: null };
    case 'this-year':
      return {
        from: toISODate(new Date(year, 0, 1)),
        to: toISODate(new Date(year + 1, 0, 1)),
      };
    case 'last-year':
      return {
        from: toISODate(new Date(year - 1, 0, 1)),
        to: toISODate(new Date(year, 0, 1)),
      };
    case 'custom':
      return {
        from: state.releaseDateFrom ? toISODate(parseDateInput(state.releaseDateFrom)) : null,
        to: state.releaseDateTo ? toISODate(addDays(parseDateInput(state.releaseDateTo), 1)) : null,
      };
    case 'all':
    default:
      return { from: null, to: null };
  }
}

export type PageFilterOptions = {
  pageIndex: number;
  pageSize: number;
  isGamesMode: boolean;
};

/**
 * Build the `MediaFilter` the library page sends to the backend on every
 * paged fetch. Pure: just reads the state and the page-mode context.
 */
export function buildPageFilter(
  state: LibraryFilterState,
  options: PageFilterOptions,
  now: Date = new Date(),
): MediaFilter {
  const filter = new MediaFilter();
  filter.Paging.PageIndex = options.pageIndex;
  filter.Paging.Count = options.pageSize;
  filter.SearchString = state.searchTerm.trim();
  filter.Ownership = state.ownershipFilter;
  filter.SortBy = state.sortMode;
  filter.SmartFilter = state.smartFilter;
  const range = releaseDateRange(state, now);
  filter.From = range.from;
  filter.To = range.to;
  if (state.statusFilter !== 'all') {
    if (options.isGamesMode) {
      filter.Status = Number(state.statusFilter);
    } else {
      filter.MediaStatus = Number(state.statusFilter);
    }
  }
  if (options.isGamesMode && state.platformFilter !== 'all') {
    filter.Platform = Number(state.platformFilter);
  }
  return filter;
}

/** Parse a `<input type="date">` value into a local-midnight Date. */
function parseDateInput(value: string): Date {
  const [year, month, day] = value.split('-').map(Number);
  return new Date(year, (month || 1) - 1, day || 1);
}
