import { TestBed } from '@angular/core/testing';

import { MEDIA_MODE_OPTIONS } from 'src/app/shared/services/media-mode.service';
import { MediaItem, UserMediaEntry } from '../models/media-item.model';
import { GameStatus } from '../models/library-status.model';
import { LibraryIntelligenceService } from './library-intelligence.service';

function makeItem(overrides: Partial<MediaItem> = {}): MediaItem {
  return {
    id: 0,
    name: '',
    description: '',
    releaseDate: null,
    genre: '',
    image: null,
    kind: 'games',
    libraryEntry: null,
    platforms: 0,
    playtime: 0,
    ...overrides,
  };
}

function makeEntry(overrides: Partial<UserMediaEntry> = {}): UserMediaEntry {
  return {
    id: 0,
    rating: null,
    startDate: null,
    endDate: null,
    status: GameStatus.Planned,
    timeSpend: 0,
    ...overrides,
  };
}

describe('LibraryIntelligenceService', () => {
  let service: LibraryIntelligenceService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(LibraryIntelligenceService);
  });

  it('summarizes game backlog health and picks the best finish candidate', () => {
    const mode = MEDIA_MODE_OPTIONS.find((option) => option.id === 'games')!;
    const short = makeItem({
      id: 1,
      name: 'Short',
      playtime: 8,
      libraryEntry: makeEntry({ id: 11, status: GameStatus.Planned, timeSpend: 0 }),
    });
    const abandoned = makeItem({
      id: 2,
      name: 'Abandoned',
      playtime: 30,
      libraryEntry: makeEntry({ id: 12, status: GameStatus.OnHold, timeSpend: 5 }),
    });
    const active = makeItem({
      id: 3,
      name: 'Active',
      playtime: 24,
      libraryEntry: makeEntry({ id: 13, status: GameStatus.Playing, timeSpend: 18, rating: 9 }),
    });
    const completed = makeItem({
      id: 4,
      name: 'Done',
      playtime: 20,
      libraryEntry: makeEntry({ id: 14, status: GameStatus.Completed, timeSpend: 20 }),
    });
    const catalog = makeItem({ id: 5, name: 'Catalog', playtime: 6 });

    const summary = service.summarize([short, abandoned, active, completed, catalog], mode);

    expect(summary.totalGames).toBe(5);
    expect(summary.ownedGames).toBe(4);
    expect(summary.playingGames).toBe(1);
    expect(summary.remainingHours).toBe(39);
    expect(summary.shortGameCount).toBe(2);
    expect(summary.abandonedGameCount).toBe(1);
    expect(summary.nextBestGame).toBe(active);
  });

  it('handles watched media using watch statuses and minutes', () => {
    const mode = MEDIA_MODE_OPTIONS.find((option) => option.id === 'animes')!;
    const watching = makeItem({
      id: 1,
      name: 'Weekly',
      kind: 'animes',
      expectedWatchTimeMinutes: 300,
      libraryEntry: makeEntry({ id: 21, status: 2, currentWatchTimeMinutes: 120 }),
    });
    const dropped = makeItem({
      id: 2,
      name: 'Dropped',
      kind: 'animes',
      expectedWatchTimeMinutes: 300,
      libraryEntry: makeEntry({ id: 22, status: 4, currentWatchTimeMinutes: 60 }),
    });

    const summary = service.summarize([watching, dropped], mode);

    expect(summary.playingGames).toBe(1);
    expect(summary.remainingHours).toBe(7);
    expect(summary.abandonedGameCount).toBe(0);
    expect(summary.nextBestGame).toBe(watching);
  });
});
