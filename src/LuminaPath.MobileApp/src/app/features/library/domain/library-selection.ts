import { MediaItem } from '../models/media-item.model';

export function selectableLibraryItems(items: readonly MediaItem[]): MediaItem[] {
  return items.filter((item) => !!item.libraryEntry);
}

export function selectedMediaItems(items: readonly MediaItem[], selectedIds: ReadonlySet<number>): MediaItem[] {
  return items.filter((item) => selectedIds.has(item.id));
}

export function selectedLibraryItems(items: readonly MediaItem[], selectedIds: ReadonlySet<number>): MediaItem[] {
  return selectableLibraryItems(selectedMediaItems(items, selectedIds));
}

export function toggleSelectedMediaId(
  selectedIds: ReadonlySet<number>,
  item: MediaItem,
): Set<number> {
  if (!item.libraryEntry) {
    return new Set(selectedIds);
  }

  const next = new Set(selectedIds);
  if (next.has(item.id)) {
    next.delete(item.id);
  } else {
    next.add(item.id);
  }
  return next;
}

export function selectVisibleLibraryItemIds(
  selectedIds: ReadonlySet<number>,
  visibleItems: readonly MediaItem[],
): Set<number> {
  const next = new Set(selectedIds);
  for (const item of selectableLibraryItems(visibleItems)) {
    next.add(item.id);
  }
  return next;
}

export function pruneSelectedMediaIds(
  selectedIds: ReadonlySet<number>,
  availableItems: readonly MediaItem[],
): Set<number> {
  const availableIds = new Set(selectableLibraryItems(availableItems).map((item) => item.id));
  const next = new Set<number>();
  for (const id of selectedIds) {
    if (availableIds.has(id)) {
      next.add(id);
    }
  }
  return next;
}

export function bulkSelectionSummary(
  visibleLibraryItemCount: number,
  selectedCount: number,
  singularLabel: string,
  pluralLabel: string,
): string {
  if (visibleLibraryItemCount === 0) {
    return `No ${pluralLabel} from this result set are in your library.`;
  }

  return selectedCount === 1
    ? `1 ${singularLabel} selected`
    : `${selectedCount} ${pluralLabel} selected`;
}
