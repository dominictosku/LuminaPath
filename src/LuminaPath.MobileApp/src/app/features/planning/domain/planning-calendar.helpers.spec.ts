import {
  dateKey,
  hasMaterializedOccurrenceForDay,
  projectedRecurringOccurrenceForDay,
  shiftForRecurrence,
  startOfCalendarMonthGrid,
  startOfWeek,
} from './planning-calendar.helpers';

describe('planning calendar helpers', () => {
  it('uses Monday-first week and month-grid starts', () => {
    expect(dateKey(startOfWeek(new Date(2026, 5, 7)))).toBe('2026-06-01');
    expect(dateKey(startOfCalendarMonthGrid(new Date(2026, 7, 15)))).toBe('2026-07-27');
  });

  it('shifts recurring dates while preserving the local time', () => {
    const value = new Date(2026, 0, 31, 18, 45);

    expect(dateKey(shiftForRecurrence(value, 'daily'))).toBe('2026-02-01');
    expect(dateKey(shiftForRecurrence(value, 'weekly'))).toBe('2026-02-07');
    expect(dateKey(shiftForRecurrence(value, 'monthly'))).toBe('2026-02-28');
    expect(shiftForRecurrence(value, 'monthly').getHours()).toBe(18);
    expect(shiftForRecurrence(value, 'monthly').getMinutes()).toBe(45);
  });

  it('projects recurring items onto matching future days only', () => {
    const sourceStart = new Date(2026, 5, 4, 18, 30);
    const sourceEnd = new Date(2026, 5, 4, 19, 30);

    const projected = projectedRecurringOccurrenceForDay({
      recurrence: 'weekly',
      completed: false,
      scheduledStartAt: sourceStart.toISOString(),
      scheduledEndAt: sourceEnd.toISOString(),
    }, new Date(2026, 5, 11));

    expect(projected).not.toBeNull();
    expect(dateKey(projected!.start)).toBe('2026-06-11');
    expect(projected!.start.getHours()).toBe(18);
    expect(projectedRecurringOccurrenceForDay({
      recurrence: 'weekly',
      completed: true,
      scheduledStartAt: sourceStart.toISOString(),
      scheduledEndAt: sourceEnd.toISOString(),
    }, new Date(2026, 5, 11))).toBeNull();
    expect(projectedRecurringOccurrenceForDay({
      recurrence: 'weekly',
      completed: false,
      scheduledStartAt: sourceStart.toISOString(),
      scheduledEndAt: sourceEnd.toISOString(),
    }, new Date(2026, 5, 10))).toBeNull();
  });

  it('projects from series schedule when an occurrence has local overrides', () => {
    const occurrenceStart = new Date(2026, 5, 4, 21, 0);
    const occurrenceEnd = new Date(2026, 5, 4, 22, 0);
    const seriesStart = new Date(2026, 5, 4, 18, 30);
    const seriesEnd = new Date(2026, 5, 4, 19, 30);

    const projected = projectedRecurringOccurrenceForDay({
      recurrence: 'none',
      seriesRecurrence: 'weekly',
      completed: false,
      scheduledStartAt: occurrenceStart.toISOString(),
      scheduledEndAt: occurrenceEnd.toISOString(),
      seriesScheduledStartAt: seriesStart.toISOString(),
      seriesScheduledEndAt: seriesEnd.toISOString(),
    }, new Date(2026, 5, 11));

    expect(projected).not.toBeNull();
    expect(dateKey(projected!.start)).toBe('2026-06-11');
    expect(projected!.start.getHours()).toBe(18);
    expect(projected!.start.getMinutes()).toBe(30);
  });

  it('does not project materialized one-off occurrences and can detect their original date', () => {
    const items = [
      {
        id: 1,
        questSeriesId: 20,
        recurrence: 'weekly' as const,
        projectsQuestSeries: true,
        seriesOccurrenceDate: '2026-06-04T00:00:00.000Z',
        scheduledStartAt: '2026-06-04T18:30:00.000Z',
        scheduledEndAt: '2026-06-04T19:30:00.000Z',
      },
      {
        id: 2,
        questSeriesId: 20,
        recurrence: 'weekly' as const,
        projectsQuestSeries: false,
        seriesOccurrenceDate: '2026-06-11T00:00:00.000Z',
        scheduledStartAt: '2026-06-12T20:00:00.000Z',
        scheduledEndAt: '2026-06-12T21:00:00.000Z',
      },
    ];

    expect(projectedRecurringOccurrenceForDay(items[1], new Date(2026, 5, 18))).toBeNull();
    expect(hasMaterializedOccurrenceForDay(items, items[0], new Date(2026, 5, 11))).toBeTrue();
  });
});
