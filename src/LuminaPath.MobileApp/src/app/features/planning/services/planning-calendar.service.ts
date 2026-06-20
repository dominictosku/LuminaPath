import { Injectable } from '@angular/core';

import { startOfDay } from 'src/app/shared/utils/date-helpers';
import { Game } from '../../games/models/games.model';
import { Quest } from '../../quests/services/quest-board.service';
import {
  dateKey as calendarDateKey,
  hasMaterializedOccurrenceForDay,
  projectedRecurringOccurrenceForDay,
  recurringItemRecurrence,
  sameDay,
  startOfCalendarMonthGrid,
  startOfWeek,
  validDate,
} from '../domain/planning-calendar.helpers';
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
  startAt?: string | null;
  endAt?: string | null;
  occurrenceDate?: string | null;
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
      ? startOfWeek(input.anchor)
      : startOfCalendarMonthGrid(input.anchor);
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
          key: calendarDateKey(date),
          date,
          number: date.getDate(),
          label: new Intl.DateTimeFormat('en', { weekday: 'short' }).format(date),
          isToday: sameDay(date, today),
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
      const key = calendarDateKey(date);
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
    return calendarDateKey(value);
  }

  private eventsForDay(date: Date, input: CalendarBuildInput): TimelineEvent[] {
    const events: TimelineEvent[] = [];

    for (const session of input.sessions) {
      const scheduledAt = new Date(session.scheduledAt);
      if (!sameDay(scheduledAt, date)) {
        continue;
      }
      events.push({
        id: `session-${session.id}`,
        sourceId: session.id,
        kind: 'session',
        title: session.gameName ?? 'Gaming session',
        subtitle: session.notes || this.formatDuration(session.durationMinutes),
        timeLabel: this.formatTime(session.scheduledAt),
        startAt: session.scheduledAt,
        endAt: null,
        occurrenceDate: null,
        recurrence: null,
        projected: false,
        completed: session.completed,
      });
    }

    for (const game of input.games) {
      const releaseDate = validDate(game.releaseDate);
      if (!releaseDate || !sameDay(releaseDate, date)) {
        continue;
      }
      events.push({
        id: `release-${game.id}`,
        sourceId: game.id,
        kind: 'release',
        title: game.name,
        subtitle: 'Release',
        timeLabel: 'Release',
        startAt: null,
        endAt: null,
        occurrenceDate: null,
        recurrence: null,
        projected: false,
        completed: releaseDate < this.startOfToday(),
      });
    }

    for (const quest of input.quests) {
      const questDate = validDate(quest.dueDate) ?? validDate(quest.scheduledStartAt);
      if (questDate && sameDay(questDate, date)) {
        events.push({
          id: `quest-${quest.id}`,
          sourceId: quest.id,
          kind: 'quest',
          title: quest.title,
          subtitle: quest.gameName ?? quest.skillName ?? this.questPriorityLabel(quest.priority),
          timeLabel: quest.completed ? 'Done' : quest.scheduledStartAt ? this.formatTime(quest.scheduledStartAt) : this.questPriorityLabel(quest.priority),
          startAt: quest.scheduledStartAt ?? quest.dueDate ?? null,
          endAt: quest.scheduledEndAt ?? null,
          occurrenceDate: null,
          recurrence: quest.recurrence,
          projected: false,
          completed: quest.completed,
        });
      }

      const projected = hasMaterializedOccurrenceForDay(input.quests, quest, date)
        ? null
        : projectedRecurringOccurrenceForDay(quest, date);
      if (projected) {
        const recurrence = recurringItemRecurrence(quest);
        events.push({
          id: `quest-${quest.id}-projected-${calendarDateKey(date)}`,
          sourceId: quest.id,
          kind: 'quest',
          title: quest.title,
          subtitle: 'Projected repeat',
          timeLabel: this.formatTime(projected.start.toISOString()),
          startAt: projected.start.toISOString(),
          endAt: projected.end.toISOString(),
          occurrenceDate: calendarDateKey(date),
          recurrence: recurrence ?? quest.recurrence,
          projected: true,
          completed: false,
        });
      }
    }

    return events.sort((a, b) => this.eventRank(a) - this.eventRank(b) || a.title.localeCompare(b.title));
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
}
