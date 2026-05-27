import {
  LIBRARY_LIST_INITIAL_WINDOW_SIZE,
  LIBRARY_LIST_ROW_GAP,
  LIBRARY_LIST_ROW_HEIGHT,
  LIBRARY_LIST_ROW_STRIDE,
  computeLibraryListWindow,
} from './library-virtual-list';

describe('library virtual list', () => {
  it('returns an empty window for no items', () => {
    expect(computeLibraryListWindow({
      totalItems: 0,
      scrollTop: 0,
      viewportHeight: 800,
      offsetTop: 0,
    })).toEqual({ start: 0, end: 0, totalHeight: 0 });
  });

  it('renders an initial slab before the list offset is measured', () => {
    const window = computeLibraryListWindow({
      totalItems: 100,
      scrollTop: 0,
      viewportHeight: 800,
      offsetTop: 0,
    });

    expect(window.start).toBe(0);
    expect(window.end).toBe(LIBRARY_LIST_INITIAL_WINDOW_SIZE);
    expect(window.totalHeight).toBe(100 * LIBRARY_LIST_ROW_HEIGHT + 99 * LIBRARY_LIST_ROW_GAP);
  });

  it('computes a buffered visible window after measurement', () => {
    const window = computeLibraryListWindow({
      totalItems: 100,
      scrollTop: 1000,
      viewportHeight: 300,
      offsetTop: 100,
    });

    expect(window.start).toBe(4);
    expect(window.end).toBe(16);
    expect(window.totalHeight).toBe(100 * LIBRARY_LIST_ROW_STRIDE - LIBRARY_LIST_ROW_GAP);
  });
});
