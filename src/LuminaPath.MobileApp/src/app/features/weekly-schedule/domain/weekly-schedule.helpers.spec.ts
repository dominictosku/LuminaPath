import {
  buildWeekDays,
  dateAtMinutes,
  dateKey,
  formatTimeRange,
  layoutOverlappingBlocks,
  startOfWeek,
  weekTitle,
  type WeekScheduleBlock,
} from './weekly-schedule.helpers';

describe('weekly schedule helpers', () => {
  it('builds a Monday-first week around the selected date', () => {
    const anchor = new Date(2026, 5, 7, 14, 20);
    const days = buildWeekDays(anchor, anchor);

    expect(dateKey(startOfWeek(anchor))).toBe('2026-06-01');
    expect(days.map((day) => day.key)).toEqual([
      '2026-06-01',
      '2026-06-02',
      '2026-06-03',
      '2026-06-04',
      '2026-06-05',
      '2026-06-06',
      '2026-06-07',
    ]);
    expect(days[6].isToday).toBeTrue();
  });

  it('creates slot dates from minutes since midnight', () => {
    const value = dateAtMinutes(new Date(2026, 5, 2), 19 * 60 + 30);

    expect(value.getFullYear()).toBe(2026);
    expect(value.getMonth()).toBe(5);
    expect(value.getDate()).toBe(2);
    expect(value.getHours()).toBe(19);
    expect(value.getMinutes()).toBe(30);
  });

  it('keeps overlapping blocks in separate lanes', () => {
    const blocks: WeekScheduleBlock[] = [
      block('quest-1', '2026-06-02T08:00:00.000Z', '2026-06-02T09:30:00.000Z'),
      block('quest-2', '2026-06-02T09:00:00.000Z', '2026-06-02T10:00:00.000Z'),
      block('quest-3', '2026-06-02T10:00:00.000Z', '2026-06-02T11:00:00.000Z'),
    ];

    const arranged = layoutOverlappingBlocks(blocks);

    expect(arranged[0].lane).toBe(0);
    expect(arranged[0].laneCount).toBe(2);
    expect(arranged[1].lane).toBe(1);
    expect(arranged[1].laneCount).toBe(2);
    expect(arranged[2].lane).toBe(0);
    expect(arranged[2].laneCount).toBe(1);
  });

  it('formats useful labels for the week and time range', () => {
    expect(weekTitle(new Date(2026, 5, 7))).toBe('Jun 1 - 7, 2026');
    expect(formatTimeRange(
      new Date(2026, 5, 2, 8, 0).toISOString(),
      new Date(2026, 5, 2, 9, 30).toISOString(),
    )).toContain('09:30');
  });
});

function block(id: string, startAt: string, endAt: string): WeekScheduleBlock {
  return {
    id,
    kind: 'quest',
    title: id,
    subtitle: '',
    startAt,
    endAt,
    color: '#7c3aed',
    completed: false,
    lane: 0,
    laneCount: 1,
  };
}
