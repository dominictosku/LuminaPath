import { TestBed } from '@angular/core/testing';

import { LibraryFilterPreset } from '../models/library-filter.model';
import { LibraryFilterPresetService } from './library-filter-preset.service';

const STORAGE_KEY = 'luminapath.library.presets.v1.games';

function makePreset(overrides: Partial<LibraryFilterPreset> = {}): LibraryFilterPreset {
  return {
    id: 'preset-1',
    name: 'Short games',
    mediaModeId: 'games',
    searchTerm: '',
    ownershipFilter: 'mine',
    statusFilter: 'all',
    platformFilter: 'all',
    releaseDateFilter: 'all',
    releaseDateFrom: '',
    releaseDateTo: '',
    sortMode: 'title',
    smartFilter: 'short',
    ...overrides,
  };
}

describe('LibraryFilterPresetService', () => {
  let service: LibraryFilterPresetService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(LibraryFilterPresetService);
  });

  it('loads saved presets for a media mode', () => {
    localStorage.setItem(STORAGE_KEY, JSON.stringify([makePreset()]));

    const presets = service.load('games');

    expect(presets.length).toBe(1);
    expect(presets[0].name).toBe('Short games');
  });

  it('ignores corrupt storage values', () => {
    localStorage.setItem(STORAGE_KEY, '{not-json');

    expect(service.load('games')).toEqual([]);
  });

  it('persists presets per media mode', () => {
    service.save('games', [makePreset({ name: 'Best next' })]);

    const raw = localStorage.getItem(STORAGE_KEY);
    expect(raw).toContain('Best next');
  });
});
