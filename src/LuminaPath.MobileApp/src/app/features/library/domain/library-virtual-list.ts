export const LIBRARY_LIST_ROW_HEIGHT = 96;
export const LIBRARY_LIST_ROW_GAP = 10;
export const LIBRARY_LIST_ROW_STRIDE = LIBRARY_LIST_ROW_HEIGHT + LIBRARY_LIST_ROW_GAP;
export const LIBRARY_LIST_BUFFER_ROWS = 4;
export const LIBRARY_LIST_INITIAL_WINDOW_SIZE = 24;

export type LibraryListWindow = {
  start: number;
  end: number;
  totalHeight: number;
};

export type LibraryListWindowInput = {
  totalItems: number;
  scrollTop: number;
  viewportHeight: number;
  offsetTop: number;
};

export function computeLibraryListWindow(input: LibraryListWindowInput): LibraryListWindow {
  const total = input.totalItems;
  const totalHeight =
    total === 0
      ? 0
      : total * LIBRARY_LIST_ROW_HEIGHT + (total - 1) * LIBRARY_LIST_ROW_GAP;

  if (total === 0) {
    return { start: 0, end: 0, totalHeight };
  }

  if (input.offsetTop === 0) {
    return {
      start: 0,
      end: Math.min(total, LIBRARY_LIST_INITIAL_WINDOW_SIZE),
      totalHeight,
    };
  }

  const startY = Math.max(0, input.scrollTop - input.offsetTop);
  const endY = startY + input.viewportHeight;
  const start = Math.max(
    0,
    Math.floor(startY / LIBRARY_LIST_ROW_STRIDE) - LIBRARY_LIST_BUFFER_ROWS,
  );
  const end = Math.min(
    total,
    Math.ceil(endY / LIBRARY_LIST_ROW_STRIDE) + LIBRARY_LIST_BUFFER_ROWS,
  );

  return { start, end, totalHeight };
}
