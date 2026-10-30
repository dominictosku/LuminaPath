import { progressSummary, statusLabel } from './dashboard-view.helpers';
import { DashboardMediaItem } from '../models/dashboard.model';

describe('Dashboard progress copy', () => {
  it('keeps logged game time distinct from completion', () => {
    const item = { kind: 'Game', status: 2, playedHours: 30, estimatedHours: 20, remainingHours: 0 } as DashboardMediaItem;
    expect(progressSummary(item)).toBe('30h logged · ~0h remaining');
    expect(statusLabel(item)).toBe('Playing');
  });
  it('uses episodes when available', () => {
    expect(progressSummary({ kind: 'Series', currentEpisode: 6, episodeCount: 12 } as DashboardMediaItem)).toBe('Episode 6 of 12');
  });
  it('does not present unknown length as zero hours remaining', () => {
    expect(progressSummary({ kind: 'Game', playedHours: 12, estimatedHours: 0 } as DashboardMediaItem)).toBe('12h logged · Length unknown');
  });
});
