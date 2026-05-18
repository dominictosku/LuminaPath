import { Component, OnChanges, SimpleChanges, inject, input } from '@angular/core';
import {
  IonBadge,
  IonButton,
  IonIcon,
  IonSkeletonText,
} from '@ionic/angular/standalone';
import { EmptyStateComponent } from 'src/app/shared/components/empty-state/empty-state.component';
import { addIcons } from 'ionicons';
import { newspaperOutline, openOutline, refreshOutline } from 'ionicons/icons';
import { firstValueFrom } from 'rxjs';

import { GameNewsItem } from 'src/app/features/games/models/games.model';
import { GameService } from 'src/app/features/games/services/game.service';
import { formatShortDate } from 'src/app/shared/utils/format';

@Component({
  selector: 'app-game-news',
  templateUrl: './game-news.component.html',
  styleUrls: ['../../pages/my-game-details.page.scss'],
  imports: [IonBadge, IonButton, IonIcon, IonSkeletonText, EmptyStateComponent],
})
export class GameNewsComponent implements OnChanges {
  private readonly gameService = inject(GameService);

  readonly gameId = input.required<number>();
  readonly gameName = input<string>('');

  newsItems: GameNewsItem[] = [];
  isLoading = false;
  errorMessage = '';
  loaded = false;
  selectedProvider: string | null = null;
  readonly skeletonRows = [1, 2, 3];

  constructor() {
    addIcons({ newspaperOutline, openOutline, refreshOutline });
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['gameId']) {
      this.newsItems = [];
      this.loaded = false;
      this.selectedProvider = null;
      this.errorMessage = '';
      void this.load();
    }
  }

  async load(refresh = false): Promise<void> {
    const id = this.gameId();
    if (!id) return;

    this.isLoading = true;
    this.errorMessage = '';

    try {
      this.newsItems = await firstValueFrom(this.gameService.getNews(id, refresh));
      this.loaded = true;
      this.selectedProvider = null;
    } catch {
      this.errorMessage = 'News could not be loaded right now.';
      this.newsItems = [];
      this.loaded = true;
    } finally {
      this.isLoading = false;
    }
  }

  newsDateLabel(item: GameNewsItem): string {
    return formatShortDate(item.publishedAt, 'Recent');
  }

  providerLabel(item: GameNewsItem): string {
    return item.provider === 'GoogleNews' ? 'Google News' : item.provider;
  }

  get newsProviderFilters(): { value: string; label: string; count: number }[] {
    const counts = new Map<string, { label: string; count: number }>();
    for (const item of this.newsItems) {
      const value = item.provider || 'Unknown';
      const label = this.providerLabel(item) || value;
      const current = counts.get(value);
      if (current) {
        current.count += 1;
      } else {
        counts.set(value, { label, count: 1 });
      }
    }
    return Array.from(counts, ([value, info]) => ({ value, label: info.label, count: info.count }))
      .sort((a, b) => b.count - a.count);
  }

  get filteredNewsItems(): GameNewsItem[] {
    if (!this.selectedProvider) return this.newsItems;
    return this.newsItems.filter((item) => (item.provider || 'Unknown') === this.selectedProvider);
  }

  setProvider(value: string | null): void {
    this.selectedProvider = value;
  }

  trackByNews(_: number, item: GameNewsItem): string {
    return item.url || item.title;
  }
}
