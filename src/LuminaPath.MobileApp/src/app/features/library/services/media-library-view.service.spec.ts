import { TestBed } from '@angular/core/testing';
import { MEDIA_MODE_OPTIONS } from 'src/app/shared/services/media-mode.service';
import { MediaItem } from '../models/media-item.model';
import { MediaLibraryViewService } from './media-library-view.service';

function mode(id: 'games' | 'animes' | 'movies' | 'series') {
  return MEDIA_MODE_OPTIONS.find((option) => option.id === id)!;
}

function mediaItem(overrides: Partial<MediaItem> = {}): MediaItem {
  return {
    id: 1,
    name: 'Test',
    description: '',
    releaseDate: null,
    genre: '',
    image: null,
    kind: 'series',
    libraryEntry: null,
    ...overrides,
  };
}

describe('MediaLibraryViewService', () => {
  let service: MediaLibraryViewService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(MediaLibraryViewService);
  });

  it('uses episode labels and watch-time estimates for series', () => {
    const item = mediaItem({
      expectedWatchTimeMinutes: 250,
      episodeCount: 10,
      libraryEntry: {
        id: 5,
        rating: null,
        startDate: null,
        endDate: null,
        status: 2,
        timeSpend: null,
        currentWatchTimeMinutes: 75,
        currentEpisode: 3,
      },
    });

    expect(service.mediaTypeLabel(item, mode('series'))).toBe('Episode 3/10');
    expect(service.durationLabel(item, mode('series'))).toBe('4h 10m');
    expect(service.playedLabel(item, mode('series'))).toBe('75m watched');
    expect(service.remainingLabel(item, mode('series'))).toBe('175m left');
  });

  it('uses backend platform flag values for game labels', () => {
    expect(service.platformLabel(1)).toBe('PlayStation 4');
    expect(service.platformLabel(2)).toBe('PlayStation 5');
    expect(service.platformLabel(4)).toBe('Switch');
    expect(service.platformLabel(8)).toBe('PC');
    expect(service.platformLabel(16)).toBe('XBOX');
    expect(service.platformLabel(3)).toBe('PlayStation 4, PlayStation 5');
  });

  it('only sends currentEpisode for episode-based media', () => {
    const details = service.toLibraryEntryDetails({
      status: 2,
      timeSpend: 4,
      rating: 8,
      startDate: '2026-01-01',
      endDate: '',
      currentEpisode: 6,
    }, mode('series'));

    expect(details).toEqual({
      status: 2,
      timeSpend: 4,
      rating: 8,
      startDate: '2026-01-01',
      endDate: null,
      currentEpisode: 6,
    });

    expect(service.toLibraryEntryDetails({
      status: 2,
      timeSpend: 4,
      rating: 8,
      startDate: '2026-01-01',
      endDate: '',
      currentEpisode: 6,
    }, mode('movies')).currentEpisode).toBeNull();
  });
  it('keeps imported game hours out of the editable manual-hours draft and payload', () => {
    const item = mediaItem({kind: 'games', playtime: 40, libraryEntry: {
      id: 5, rating: 9, startDate: null, endDate: null, status: 2,
      timeSpend: 8.5, myGameInfo: {trackedHours: 10},
    }});
    const draft = service.createLibraryForm(item, mode('games'));
    expect(service.playedOf(item, mode('games'))).toBe(18.5);
    expect(draft.timeSpend).toBe(8.5);
    expect(service.toLibraryEntryDetails(draft, mode('games')).timeSpend).toBe(8.5);
    expect(item.libraryEntry?.myGameInfo?.trackedHours).toBe(10);
  });

});
