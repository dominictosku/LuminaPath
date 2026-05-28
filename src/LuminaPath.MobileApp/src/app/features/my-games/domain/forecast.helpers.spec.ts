import { GameForecast } from 'src/app/features/planning/services/gaming-session.service';

import {
  forecastBarParts,
  forecastHours,
  forecastProgress,
  forecastSummary,
  forecastWidth,
} from './forecast.helpers';

function forecast(overrides: Partial<GameForecast> = {}): GameForecast {
  return {
    myGameId: 1,
    gameName: 'Test',
    playtimeEstimateHours: null,
    playedHours: 0,
    remainingHours: null,
    scheduledHours: 0,
    upcomingSessionCount: 0,
    sessionsToCompletion: null,
    projectedCompletionDate: null,
    weeklyHours: 0,
    additionalHoursNeeded: 0,
    weeksAtCurrentPace: null,
    ...overrides,
  };
}

describe('forecastHours', () => {
  it('rounds to 1 decimal and suffixes h', () => {
    expect(forecastHours(0)).toBe('0h');
    expect(forecastHours(1.24)).toBe('1.2h');
    expect(forecastHours(1.26)).toBe('1.3h');
    expect(forecastHours(40)).toBe('40h');
  });
});

describe('forecastBarParts', () => {
  it('returns all zeros when forecast is null', () => {
    expect(forecastBarParts(null)).toEqual({ played: 0, scheduled: 0, remaining: 0, total: 0 });
  });

  it('uses remainingHours directly when provided', () => {
    const parts = forecastBarParts(forecast({ playedHours: 5, scheduledHours: 3, remainingHours: 12 }));
    expect(parts).toEqual({ played: 5, scheduled: 3, remaining: 9, total: 17 });
  });

  it('caps scheduled at the remaining-hours budget', () => {
    // scheduled (50) exceeds remaining (10) → scheduled clamped to 10, remaining 0
    const parts = forecastBarParts(forecast({ playedHours: 5, scheduledHours: 50, remainingHours: 10 }));
    expect(parts.scheduled).toBe(10);
    expect(parts.remaining).toBe(0);
    expect(parts.total).toBe(15);
  });

  it('falls back to (estimate - played) when remainingHours is null', () => {
    const parts = forecastBarParts(forecast({
      playedHours: 4,
      scheduledHours: 0,
      remainingHours: null,
      playtimeEstimateHours: 10,
    }));
    expect(parts.remaining).toBe(6);
    expect(parts.total).toBe(10);
  });

  it('clamps every segment to non-negative', () => {
    const parts = forecastBarParts(forecast({
      playedHours: 20,
      scheduledHours: 0,
      remainingHours: null,
      playtimeEstimateHours: 10, // played > estimate
    }));
    expect(parts.played).toBe(20);
    expect(parts.remaining).toBe(0);
  });
});

describe('forecastWidth', () => {
  it('is 0 when total is 0', () => {
    expect(forecastWidth(null, 'played')).toBe(0);
    expect(forecastWidth(forecast({ remainingHours: 0 }), 'remaining')).toBe(0);
  });

  it('returns percentage with one decimal', () => {
    // 5 / 10 = 50.0; 3 / 10 = 30.0; 2 / 10 = 20.0
    const f = forecast({ playedHours: 5, scheduledHours: 3, remainingHours: 5 });
    expect(forecastWidth(f, 'played')).toBe(50);
    expect(forecastWidth(f, 'scheduled')).toBe(30);
    expect(forecastWidth(f, 'remaining')).toBe(20);
  });
});

describe('forecastProgress', () => {
  it('is 0 when there is no playtime estimate', () => {
    expect(forecastProgress(null)).toBe(0);
    expect(forecastProgress(forecast({ playtimeEstimateHours: null }))).toBe(0);
    expect(forecastProgress(forecast({ playtimeEstimateHours: 0 }))).toBe(0);
  });

  it('clamps at 1 when overplayed', () => {
    expect(forecastProgress(forecast({ playtimeEstimateHours: 10, playedHours: 30 }))).toBe(1);
  });

  it('returns the played/estimate ratio otherwise', () => {
    expect(forecastProgress(forecast({ playtimeEstimateHours: 10, playedHours: 3 }))).toBeCloseTo(0.3, 5);
  });
});

describe('forecastSummary', () => {
  it('is empty when forecast is null', () => {
    expect(forecastSummary(null)).toBe('');
  });

  it('prompts for an estimate when remainingHours is null', () => {
    expect(forecastSummary(forecast({ remainingHours: null }))).toBe('Add a playtime estimate to see a forecast.');
  });

  it('flags overplayed when remainingHours <= 0', () => {
    expect(forecastSummary(forecast({ remainingHours: 0 }))).toBe('You are already past the estimated playtime.');
  });

  it('renders an ETA line when projectedCompletionDate is present', () => {
    const out = forecastSummary(forecast({
      remainingHours: 10,
      projectedCompletionDate: '2026-06-15T00:00:00Z',
      sessionsToCompletion: 4,
    }));
    expect(out).toContain('4 sessions to finish');
    expect(out).toContain('ETA');
  });

  it('uses pace-based phrasing when weeksAtCurrentPace is set', () => {
    const out = forecastSummary(forecast({
      remainingHours: 10,
      additionalHoursNeeded: 8,
      weeksAtCurrentPace: 2,
      weeklyHours: 4,
    }));
    expect(out).toBe('Need 8h more · ~2 weeks at 4h/week');
  });

  it('falls back to "schedule sessions" when there is no pace yet', () => {
    const out = forecastSummary(forecast({
      remainingHours: 5,
      additionalHoursNeeded: 5,
    }));
    expect(out).toBe('Need 5h more — schedule sessions to project an ETA.');
  });
});
