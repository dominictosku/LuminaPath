import { MediaLibraryViewService } from './media-library-view.service';
import { MEDIA_MODE_OPTIONS } from 'src/app/shared/services/media-mode.service';
import { MediaItem } from '../models/media-item.model';

describe('Tracking presentation', () => {
  const view = new MediaLibraryViewService();
  const gameMode = MEDIA_MODE_OPTIONS.find(mode => mode.id === 'games')!;
  const seriesMode = MEDIA_MODE_OPTIONS.find(mode => mode.id === 'series')!;
  it('does not mistake exceeding an estimate for completion', () => {
    const item = { playtime: 10, libraryEntry: { status: 2, timeSpend: 20 } } as MediaItem;
    expect(view.measuredProgress(item, gameMode)).toBeNull();
    expect(view.statusLabel(item, gameMode)).toBe('Playing');
    expect(view.trackingSummary(item, gameMode)).toBe('20h played · ~0h remaining');
  });
  it('shows unknown length rather than a fabricated percentage', () => {
    const item = { playtime: 0, libraryEntry: { status: 2 } } as MediaItem;
    expect(view.remainingLabel(item, gameMode)).toBe('Length unknown');
    expect(view.measuredProgress(item, seriesMode)).toBeNull();
  });
  it('measures episode progress independently of estimated watch time', () => {
    const item = { episodeCount: 12, libraryEntry: { currentEpisode: 6, status: 2 } } as MediaItem;
    expect(view.trackingSummary(item, seriesMode)).toBe('Episode 6 of 12');
    expect(view.measuredProgress(item, seriesMode)).toBe(50);
  });
});
