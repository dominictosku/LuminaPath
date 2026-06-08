import {
  dateKey,
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
});
