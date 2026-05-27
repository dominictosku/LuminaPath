import {
  bulkSelectionSummary,
  pruneSelectedMediaIds,
  selectVisibleLibraryItemIds,
  selectableLibraryItems,
  selectedLibraryItems,
  selectedMediaItems,
  toggleSelectedMediaId,
} from './library-selection';
import { MediaItem, UserMediaEntry } from '../models/media-item.model';

function entry(overrides: Partial<UserMediaEntry> = {}): UserMediaEntry {
  return {
    id: 1,
    rating: null,
    startDate: null,
    endDate: null,
    status: 0,
    timeSpend: null,
    ...overrides,
  };
}

function item(id: number, hasLibraryEntry = true): MediaItem {
  return {
    id,
    name: `Item ${id}`,
    description: '',
    releaseDate: null,
    genre: '',
    image: null,
    kind: 'games',
    libraryEntry: hasLibraryEntry ? entry({ id: id * 10 }) : null,
  };
}

describe('library selection helpers', () => {
  it('filters and resolves selected library items', () => {
    const items = [item(1), item(2, false), item(3)];
    const selected = new Set([2, 3]);

    expect(selectableLibraryItems(items).map((value) => value.id)).toEqual([1, 3]);
    expect(selectedMediaItems(items, selected).map((value) => value.id)).toEqual([2, 3]);
    expect(selectedLibraryItems(items, selected).map((value) => value.id)).toEqual([3]);
  });

  it('toggles only selectable items', () => {
    expect(Array.from(toggleSelectedMediaId(new Set(), item(1)))).toEqual([1]);
    expect(Array.from(toggleSelectedMediaId(new Set([1]), item(1)))).toEqual([]);
    expect(Array.from(toggleSelectedMediaId(new Set([1]), item(2, false)))).toEqual([1]);
  });

  it('selects visible library items and prunes stale ids', () => {
    const items = [item(1), item(2, false), item(3)];

    expect(Array.from(selectVisibleLibraryItemIds(new Set([9]), items))).toEqual([9, 1, 3]);
    expect(Array.from(pruneSelectedMediaIds(new Set([1, 2, 3, 9]), items))).toEqual([1, 3]);
  });

  it('formats bulk selection summaries', () => {
    expect(bulkSelectionSummary(0, 0, 'game', 'games')).toBe(
      'No games from this result set are in your library.',
    );
    expect(bulkSelectionSummary(2, 1, 'game', 'games')).toBe('1 game selected');
    expect(bulkSelectionSummary(2, 2, 'game', 'games')).toBe('2 games selected');
  });
});
