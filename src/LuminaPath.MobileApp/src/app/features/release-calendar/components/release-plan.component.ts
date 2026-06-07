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
import { Game, platformLabelFromValue } from '../../games/models/games.model';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { EmptyStateComponent } from 'src/app/shared/components/empty-state/empty-state.component';
import { GameStatus, gameStatusLabel, isGameBacklogStatus } from '../../library/models/library-status.model';
import {
  gameStatusOf,
  progressRatioOfGame,
  remainingHoursOfGame,
} from '../../games/domain/game-library-metrics';

type ReleaseMode = 'week' | 'backlog';

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
    FormsModule,
    IonBadge,
    IonIcon,
    IonProgressBar,
    IonRange,
    IonSegment,
    IonSegmentButton,
    IonSkeletonText,
    EmptyStateComponent,
  ],
})
export class ReleasePlanComponent implements OnChanges {
  @Input() games: Game[] = [];
  @Input() isLoading = false;
  @Input() errorMessage = '';

  playingGames: Game[] = [];
  backlogGames: Game[] = [];
  metrics: PlanMetric[] = [];
  mode: ReleaseMode = 'week';
  weeklyHours = 10;

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
    return platformLabelFromValue(value);
  }

  statusLabel(game: Game): string {
    return gameStatusLabel(this.statusOf(game));
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

  private remainingOf(game: Game): number {
    return remainingHoursOfGame(game);
  }

  private statusOf(game: Game): number {
    return gameStatusOf(game);
  }
}
