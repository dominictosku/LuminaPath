import { Component, OnInit, inject } from '@angular/core';
import {
  IonContent,
  IonIcon,
  IonRefresher,
  IonRefresherContent,
} from '@ionic/angular/standalone';
import { firstValueFrom } from 'rxjs';

import { MediaFilter } from 'src/app/core/entities/mediaFilter';
import { Game } from '../../games/models/games.model';
import { GameService } from '../../games/services/game.service';
import { ReleasePlanComponent } from '../../release-calendar/components/release-plan.component';

@Component({
  selector: 'app-planning',
  templateUrl: './planning.page.html',
  styleUrls: ['./planning.page.scss'],
  imports: [
    IonContent,
    IonIcon,
    IonRefresher,
    IonRefresherContent,
    ReleasePlanComponent,
  ],
})
export class PlanningPage implements OnInit {
  private gameService = inject(GameService);

  isLoading = true;
  errorMessage = '';
  allGames: Game[] = [];

  async ngOnInit(): Promise<void> {
    await this.refresh();
  }

  async refresh(event?: CustomEvent): Promise<void> {
    this.isLoading = !event;
    this.errorMessage = '';

    try {
      const gamesResult = await firstValueFrom(this.gameService.getAll(this.createPlanFilter()));
      this.allGames = gamesResult.data ?? [];
    } catch {
      this.errorMessage = 'Release plan could not be loaded.';
    } finally {
      this.isLoading = false;
      const target = event?.target as HTMLIonRefresherElement | undefined;
      target?.complete();
    }
  }

  private createPlanFilter(): MediaFilter {
    const filter = new MediaFilter();
    filter.Paging.Count = 500;
    return filter;
  }
}
