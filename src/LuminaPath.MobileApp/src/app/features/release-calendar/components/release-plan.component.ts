import { CommonModule } from '@angular/common';
import { Component, Input, OnChanges, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  IonBadge,
  IonIcon,
  IonProgressBar,
  IonRange,
  IonSegment,
  IonSegmentButton,
  IonSkeletonText,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  calendarClearOutline,
  checkmarkCircleOutline,
  flameOutline,
  gameControllerOutline,
  hourglassOutline,
  layersOutline,
  rocketOutline,
  sparklesOutline,
  timeOutline,
} from 'ionicons/icons';
import { Game, Platforms } from '../../games/models/games.model';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { GameStatus, gameStatusLabel, isGameBacklogStatus } from '../../library/models/library-status.model';
import {
  gameStatusOf,
  playedHoursOfGame,
  progressRatioOfGame,
  releaseDateOfGame,
  remainingHoursOfGame,
} from '../../games/domain/game-library-metrics';

type ReleaseMode = 'week' | 'release' | 'backlog';

type CalendarDay = {
  label: number;
  date: Date;
  isToday: boolean;
  isCurrentMonth: boolean;
  releases: Game[];
};

type PlanMetric = {
  label: string;
  value: string;
  detail: string;
  icon: string;
};

@Component({
  selector: 'app-release-plan',
  templateUrl: './release-plan.component.html',
  styleUrls: ['./release-plan.component.scss'],
  imports: [
    CommonModule,
    FormsModule,
    IonBadge,
    IonIcon,
    IonProgressBar,
    IonRange,
    IonSegment,
    IonSegmentButton,
    IonSkeletonText,
  ],
})
export class ReleasePlanComponent implements OnChanges {
  @Input() games: Game[] = [];
  @Input() isLoading = false;
  @Input() errorMessage = '';

  playingGames: Game[] = [];
  backlogGames: Game[] = [];
  upcomingReleases: Game[] = [];
  calendarDays: CalendarDay[] = [];
  metrics: PlanMetric[] = [];
  mode: ReleaseMode = 'week';
  weeklyHours = 10;
  monthLabel = '';

  constructor() {
    addIcons({
      calendarClearOutline,
      checkmarkCircleOutline,
      flameOutline,
      gameControllerOutline,
      hourglassOutline,
      layersOutline,
      rocketOutline,
      sparklesOutline,
      timeOutline,
    });
  }

  ngOnChanges(changes: SimpleChanges) {
    if (changes['games']) {
      this.buildPlan();
    }
  }

  updateWeeklyHours() {
    this.buildMetrics();
  }

  imageFor(game: Game): string {
    return mediaImageUrl(game.image);
  }

  platformLabel(value: number | null | undefined): string {
    return Platforms.find((platform) => platform.value === Number(value))?.label ?? 'Unknown';
  }

  statusLabel(game: Game): string {
    return gameStatusLabel(this.statusOf(game));
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

  remainingLabel(game: Game): string {
    return `${Math.round(this.remainingOf(game))}h left`;
  }

  progressOf(game: Game): number {
    return progressRatioOfGame(game);
  }

  weeksFor(game: Game): number {
    return Math.max(1, Math.ceil(this.remainingOf(game) / Math.max(1, this.weeklyHours)));
  }

  trackByGameId(_: number, game: Game): number {
    return game.id;
  }

  trackByDay(_: number, day: CalendarDay): string {
    return day.date.toISOString();
  }

  private buildPlan() {
    this.playingGames = this.games
      .filter((game) => this.statusOf(game) === GameStatus.Playing)
      .sort((a, b) => this.remainingOf(a) - this.remainingOf(b))
      .slice(0, 5);

    this.backlogGames = this.games
      .filter((game) => {
        const status = this.statusOf(game);
        return !!game.myGames && isGameBacklogStatus(status);
      })
      .sort((a, b) => this.remainingOf(b) - this.remainingOf(a))
      .slice(0, 8);

    const today = this.startOfToday();
    this.upcomingReleases = this.games
      .filter((game) => this.releaseDateOf(game) >= today)
      .sort((a, b) => this.releaseDateOf(a).getTime() - this.releaseDateOf(b).getTime())
      .slice(0, 8);

    this.calendarDays = this.buildCalendarDays();
    this.buildMetrics();
  }

  private buildMetrics() {
    const remainingHours = Math.round(
      this.games
        .filter((game) => !!game.myGames)
        .reduce((sum, game) => sum + this.remainingOf(game), 0)
    );
    const weeksToClear = Math.ceil(remainingHours / Math.max(1, this.weeklyHours));
    const completed = this.games.filter((game) => this.statusOf(game) === GameStatus.Completed).length;

    this.metrics = [
      {
        label: 'Capacity',
        value: `${this.weeklyHours}h`,
        detail: 'available per week',
        icon: 'time-outline',
      },
      {
        label: 'Backlog',
        value: `${remainingHours}h`,
        detail: `${weeksToClear} weeks at this pace`,
        icon: 'hourglass-outline',
      },
      {
        label: 'Active',
        value: String(this.playingGames.length),
        detail: 'games in progress',
        icon: 'game-controller-outline',
      },
      {
        label: 'Done',
        value: String(completed),
        detail: 'completed games',
        icon: 'checkmark-circle-outline',
      },
    ];
  }

  private buildCalendarDays(): CalendarDay[] {
    const today = this.startOfToday();
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

  private playedOf(game: Game): number {
    return playedHoursOfGame(game);
  }

  private remainingOf(game: Game): number {
    return remainingHoursOfGame(game);
  }

  private statusOf(game: Game): number {
    return gameStatusOf(game);
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
