import { summarizeLibraryIntelligence } from './library-intelligence';
import { GameStatus } from '../models/library-status.model';
import { MediaItem, UserMediaEntry } from '../models/media-item.model';

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
    id: 1,
    rating: null,
    startDate: null,
    endDate: null,
    status: GameStatus.Planned,
    timeSpend: 0,
    ...overrides,
  };
}

describe('library intelligence domain', () => {
  it('summarizes backlog state without framework services', () => {
    const active = makeItem({
      id: 1,
      name: 'Active',
      playtime: 20,
      libraryEntry: makeEntry({ status: GameStatus.Playing, timeSpend: 16, rating: 9 }),
    });
    const waiting = makeItem({
      id: 2,
      name: 'Waiting',
      playtime: 8,
      libraryEntry: makeEntry({ status: GameStatus.Planned, timeSpend: 0 }),
    });
    const catalog = makeItem({ id: 3, name: 'Catalog', playtime: 4 });

    const summary = summarizeLibraryIntelligence([waiting, catalog, active], { id: 'games' });

    expect(summary.totalGames).toBe(3);
    expect(summary.ownedGames).toBe(2);
    expect(summary.playingGames).toBe(1);
    expect(summary.shortGameCount).toBe(2);
    expect(summary.nextBestGame).toBe(active);
  });
});
