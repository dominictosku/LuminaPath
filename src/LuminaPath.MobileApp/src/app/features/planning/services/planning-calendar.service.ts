import { Injectable } from '@angular/core';

import { startOfDay } from 'src/app/shared/utils/date-helpers';
import { Game } from '../../games/models/games.model';
import { Quest } from '../../quests/services/quest-board.service';
import { GamingSession } from './gaming-session.service';

export type CalendarMode = 'week' | 'month';
export type TimelineEventKind = 'session' | 'release' | 'quest';

export type TimelineEvent = {
  id: string;
  sourceId: number;
  kind: TimelineEventKind;
  title: string;
  subtitle: string;
  timeLabel: string;
  recurrence?: Quest['recurrence'] | null;
  projected?: boolean;
  completed: boolean;
};

export type PlanningCalendarDay = {
  key: string;
  date: Date;
  number: number;
  label: string;
  isToday: boolean;
  isCurrentMonth: boolean;
  events: TimelineEvent[];
};

export type PlanningCalendar = {
  title: string;
  days: PlanningCalendarDay[];
};

export type DayBucket = {
  key: string;
  label: string;
  sessions: GamingSession[];
};

type CalendarBuildInput = {
  mode: CalendarMode;
  anchor: Date;
  sessions: GamingSession[];
  games: Game[];
  quests: Quest[];
};

@Injectable({ providedIn: 'root' })
export class PlanningCalendarService {
  build(input: CalendarBuildInput): PlanningCalendar {
    const today = this.startOfToday();
    const start = input.mode === 'week'
      ? this.startOfWeek(input.anchor)
      : this.startOfCalendarMonth(input.anchor);
    const dayCount = input.mode === 'week' ? 7 : 42;
    const currentMonth = input.anchor.getMonth();

    return {
      title: input.mode === 'week'
        ? this.weekRangeLabel(start)
        : new Intl.DateTimeFormat('en', { month: 'long', year: 'numeric' }).format(input.anchor),
      days: Array.from({ length: dayCount }, (_, index) => {
        const date = new Date(start);
        date.setDate(start.getDate() + index);

        return {
          key: this.dateKey(date),
          date,
          number: date.getDate(),
          label: new Intl.DateTimeFormat('en', { weekday: 'short' }).format(date),
          isToday: this.sameDay(date, today),
          isCurrentMonth: date.getMonth() === currentMonth,
          events: this.eventsForDay(date, input),
        };
      }),
    };
  }

  groupSessionsByDay(sessions: GamingSession[]): DayBucket[] {
    const map = new Map<string, DayBucket>();

    for (const session of sessions) {
      const date = new Date(session.scheduledAt);
      const key = this.dateKey(date);
      let bucket = map.get(key);
      if (!bucket) {
        bucket = {
          key,
          label: new Intl.DateTimeFormat('en', { weekday: 'short', month: 'short', day: 'numeric' }).format(date),
          sessions: [],
        };
        map.set(key, bucket);
      }
      bucket.sessions.push(session);
    }

    return Array.from(map.values()).sort((a, b) => a.key.localeCompare(b.key));
  }

  shiftAnchor(anchor: Date, mode: CalendarMode, direction: -1 | 1): Date {
    const next = new Date(anchor);
    if (mode === 'week') {
      next.setDate(next.getDate() + direction * 7);
    } else {
      next.setMonth(next.getMonth() + direction);
    }
    return startOfDay(next);
  }

  startOfToday(): Date {
    return startOfDay(new Date());
  }

  dateKey(value: Date): string {
    return `${value.getFullYear()}-${String(value.getMonth() + 1).padStart(2, '0')}-${String(value.getDate()).padStart(2, '0')}`;
  }

  private eventsForDay(date: Date, input: CalendarBuildInput): TimelineEvent[] {
    const events: TimelineEvent[] = [];

    for (const session of input.sessions) {
      const scheduledAt = new Date(session.scheduledAt);
      if (!this.sameDay(scheduledAt, date)) {
        continue;
      }
      events.push({
        id: `session-${session.id}`,
        sourceId: session.id,
        kind: 'session',
        title: session.gameName ?? 'Gaming session',
        subtitle: session.notes || this.formatDuration(session.durationMinutes),
        timeLabel: this.formatTime(session.scheduledAt),
        recurrence: null,
        projected: false,
        completed: session.completed,
      });
    }

    for (const game of input.games) {
      const releaseDate = this.validDate(game.releaseDate);
      if (!releaseDate || !this.sameDay(releaseDate, date)) {
        continue;
      }
      events.push({
        id: `release-${game.id}`,
        sourceId: game.id,
        kind: 'release',
        title: game.name,
        subtitle: 'Release',
        timeLabel: 'Release',
        recurrence: null,
        projected: false,
        completed: releaseDate < this.startOfToday(),
      });
    }

    for (const quest of input.quests) {
      const questDate = this.validDate(quest.dueDate) ?? this.validDate(quest.scheduledStartAt);
      if (questDate && this.sameDay(questDate, date)) {
        events.push({
          id: `quest-${quest.id}`,
          sourceId: quest.id,
          kind: 'quest',
          title: quest.title,
          subtitle: quest.gameName ?? quest.skillName ?? this.questPriorityLabel(quest.priority),
          timeLabel: quest.completed ? 'Done' : quest.scheduledStartAt ? this.formatTime(quest.scheduledStartAt) : this.questPriorityLabel(quest.priority),
          recurrence: quest.recurrence,
          projected: false,
          completed: quest.completed,
        });
      }

      const projected = this.projectedQuestOccurrenceForDay(quest, date);
      if (projected) {
        events.push({
          id: `quest-${quest.id}-projected-${this.dateKey(date)}`,
          sourceId: quest.id,
          kind: 'quest',
          title: quest.title,
          subtitle: 'Projected repeat',
          timeLabel: this.formatTime(projected.start.toISOString()),
          recurrence: quest.recurrence,
          projected: true,
          completed: false,
        });
      }
    }

    return events.sort((a, b) => this.eventRank(a) - this.eventRank(b) || a.title.localeCompare(b.title));
  }

  private projectedQuestOccurrenceForDay(quest: Quest, day: Date): { start: Date; end: Date } | null {
    if (!this.hasRecurrence(quest.recurrence) || quest.completed || !quest.scheduledStartAt || !quest.scheduledEndAt) {
      return null;
    }

    const sourceStart = new Date(quest.scheduledStartAt);
    const sourceEnd = new Date(quest.scheduledEndAt);
    if (Number.isNaN(sourceStart.getTime()) || Number.isNaN(sourceEnd.getTime())) {
      return null;
    }

    if (this.sameDay(sourceStart, day) || startOfDay(day) < startOfDay(sourceStart)) {
      return null;
    }

    const projectedStart = this.projectedOccurrenceStartForDay(sourceStart, quest.recurrence, day);
    const durationMs = sourceEnd.getTime() - sourceStart.getTime();
    if (!projectedStart || durationMs <= 0) {
      return null;
    }

    return {
      start: projectedStart,
      end: new Date(projectedStart.getTime() + durationMs),
    };
  }

  private projectedOccurrenceStartForDay(sourceStart: Date, recurrence: Quest['recurrence'], day: Date): Date | null {
    const sourceDay = startOfDay(sourceStart);
    const targetDay = startOfDay(day);
    if (targetDay <= sourceDay) {
      return null;
    }

    if (recurrence === 'daily') {
      return this.dateAtMinutes(targetDay, this.minutesSinceDayStart(sourceStart));
    }

    if (recurrence === 'weekly') {
      const dayDifference = Math.round((targetDay.getTime() - sourceDay.getTime()) / 86_400_000);
      return dayDifference % 7 === 0
        ? this.dateAtMinutes(targetDay, this.minutesSinceDayStart(sourceStart))
        : null;
    }

    if (recurrence !== 'monthly') {
      return null;
    }

    let candidate = new Date(sourceStart);
    for (let index = 0; index < 240 && startOfDay(candidate) <= targetDay; index += 1) {
      candidate = this.addMonthsClamped(candidate, 1);
      if (this.sameDay(candidate, targetDay)) {
        return candidate;
      }
    }

    return null;
  }

  private hasRecurrence(recurrence: Quest['recurrence'] | null | undefined): boolean {
    return Boolean(recurrence && recurrence !== 'none');
  }

  private dateAtMinutes(day: Date, minutes: number): Date {
    const date = startOfDay(day);
    date.setMinutes(minutes);
    return date;
  }

  private minutesSinceDayStart(value: Date): number {
    return value.getHours() * 60 + value.getMinutes();
  }

  private addMonthsClamped(value: Date, months: number): Date {
    const year = value.getFullYear();
    const month = value.getMonth() + months;
    const day = value.getDate();
    const lastDayOfTargetMonth = new Date(year, month + 1, 0).getDate();
    return new Date(
      year,
      month,
      Math.min(day, lastDayOfTargetMonth),
      value.getHours(),
      value.getMinutes(),
      value.getSeconds(),
      value.getMilliseconds(),
    );
  }

  private eventRank(event: TimelineEvent): number {
    if (event.kind === 'session') return 0;
    if (event.kind === 'quest') return 1;
    return 2;
  }

  private questPriorityLabel(priority: Quest['priority']): string {
    return priority === 'high' ? 'High priority' : priority === 'low' ? 'Low priority' : 'Medium priority';
  }

  private formatDuration(minutes: number): string {
    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;
    if (hours === 0) return `${remainingMinutes}m`;
    return remainingMinutes === 0 ? `${hours}h` : `${hours}h ${remainingMinutes}m`;
  }

  private formatTime(isoString: string): string {
    const date = new Date(isoString);
    return new Intl.DateTimeFormat('en', { hour: 'numeric', minute: '2-digit' }).format(date);
  }

  private weekRangeLabel(start: Date): string {
    const end = new Date(start);
    end.setDate(start.getDate() + 6);
    const formatter = new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric' });
    return `${formatter.format(start)} - ${formatter.format(end)}`;
  }

  private startOfCalendarMonth(date: Date): Date {
    const monthStart = new Date(date.getFullYear(), date.getMonth(), 1);
    const start = new Date(monthStart);
    start.setDate(monthStart.getDate() - ((monthStart.getDay() + 6) % 7));
    return startOfDay(start);
  }

  private startOfWeek(date: Date): Date {
    const start = startOfDay(date);
    start.setDate(start.getDate() - ((start.getDay() + 6) % 7));
    return start;
  }

  private sameDay(a: Date, b: Date): boolean {
    return a.getFullYear() === b.getFullYear()
      && a.getMonth() === b.getMonth()
      && a.getDate() === b.getDate();
  }

  private validDate(value: Date | string | null | undefined): Date | null {
    if (!value) {
      return null;
    }
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? null : date;
  }
}
