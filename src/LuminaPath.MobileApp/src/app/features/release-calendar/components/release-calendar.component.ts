import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { IonBadge, IonIcon, IonSkeletonText } from '@ionic/angular/standalone';

import { EmptyStateComponent } from 'src/app/shared/components/empty-state/empty-state.component';
import { Game, platformLabelFromValue } from '../../games/models/games.model';
import { releaseDateOfGame } from '../../games/domain/game-library-metrics';

type CalendarDay = {
  label: number;
  date: Date;
  isToday: boolean;
  isCurrentMonth: boolean;
  releases: Game[];
};

@Component({
  selector: 'app-release-calendar',
  templateUrl: './release-calendar.component.html',
  styleUrls: ['./release-calendar.component.scss'],
  imports: [
    IonBadge,
    IonIcon,
    IonSkeletonText,
    EmptyStateComponent,
  ],
})
export class ReleaseCalendarComponent implements OnChanges {
  @Input() games: Game[] = [];
  @Input() isLoading = false;

  readonly weekdays = ['S', 'M', 'T', 'W', 'T', 'F', 'S'];
  readonly skeletonDays = Array.from({ length: 42 }, (_, index) => index);
  readonly skeletonCards = [1, 2, 3];

  calendarDays: CalendarDay[] = [];
  upcomingReleases: Game[] = [];
  monthLabel = '';

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['games']) {
      this.buildCalendar();
    }
  }

  platformLabel(value: number | null | undefined): string {
    return platformLabelFromValue(value);
  }

  releaseLabel(game: Game): string {
    const date = this.releaseDateOf(game);

    if (Number.isNaN(date.getTime())) {
      return 'No date';
    }

    return new Intl.DateTimeFormat('en', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    }).format(date);
  }

  trackByGameId(_: number, game: Game): number {
    return game.id;
  }

  trackByDay(_: number, day: CalendarDay): string {
    return day.date.toISOString();
  }

  private buildCalendar(): void {
    const today = this.startOfToday();

    this.upcomingReleases = this.games
      .filter((game) => this.releaseDateOf(game) >= today)
      .sort((a, b) => this.releaseDateOf(a).getTime() - this.releaseDateOf(b).getTime())
      .slice(0, 8);

    this.calendarDays = this.buildCalendarDays(today);
  }

  private buildCalendarDays(today: Date): CalendarDay[] {
    const monthStart = new Date(today.getFullYear(), today.getMonth(), 1);
    const calendarStart = new Date(monthStart);
    calendarStart.setDate(monthStart.getDate() - monthStart.getDay());

    this.monthLabel = new Intl.DateTimeFormat('en', {
      month: 'long',
      year: 'numeric',
    }).format(today);

    return Array.from({ length: 42 }, (_, index) => {
      const date = new Date(calendarStart);
      date.setDate(calendarStart.getDate() + index);

      return {
        label: date.getDate(),
        date,
        isToday: date.toDateString() === today.toDateString(),
        isCurrentMonth: date.getMonth() === today.getMonth(),
        releases: this.games.filter((game) => this.sameDay(this.releaseDateOf(game), date)).slice(0, 3),
      };
    });
  }

  private releaseDateOf(game: Game): Date {
    return releaseDateOfGame(game);
  }

  private sameDay(a: Date, b: Date): boolean {
    return !Number.isNaN(a.getTime()) && a.toDateString() === b.toDateString();
  }

  private startOfToday(): Date {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    return today;
  }
}
