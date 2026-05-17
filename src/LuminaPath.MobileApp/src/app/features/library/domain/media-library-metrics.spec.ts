import { GameStatus, WatchStatus } from '../models/library-status.model';
import { MediaItem, UserMediaEntry } from '../models/media-item.model';
import { playedOf, progressOf, remainingOf } from './media-library-metrics';

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

describe('media library metrics domain', () => {
  it('combines manual and tracked playtime for games', () => {
    const item = makeItem({
      playtime: 20,
      libraryEntry: makeEntry({
        timeSpend: 5,
        myGameInfo: { trackedHours: 3 },
      }),
    });

    expect(playedOf(item, { id: 'games' })).toBe(8);
    expect(remainingOf(item, { id: 'games' })).toBe(12);
    expect(progressOf(item, { id: 'games' })).toBe(40);
  });

  it('uses watched minutes for episode-based media', () => {
    const item = makeItem({
      kind: 'animes',
      expectedWatchTimeMinutes: 240,
      libraryEntry: makeEntry({
        status: WatchStatus.Watching,
        currentWatchTimeMinutes: 90,
      }),
    });

    expect(playedOf(item, { id: 'animes' })).toBe(1.5);
    expect(remainingOf(item, { id: 'animes' })).toBe(2.5);
    expect(progressOf(item, { id: 'animes' })).toBe(38);
  });
});
