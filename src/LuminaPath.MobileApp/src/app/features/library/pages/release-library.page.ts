import { CommonModule } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  IonBadge,
  IonButton,
  IonContent,
  IonIcon,
  IonSegment,
  IonSegmentButton,
  IonSkeletonText,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  calendarClearOutline,
  filmOutline,
  gameControllerOutline,
  peopleOutline,
  refreshOutline,
  sparklesOutline,
  timeOutline,
} from 'ionicons/icons';
import { forkJoin } from 'rxjs';
import { Platforms } from '../../games/models/games.model';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { MediaModeService } from 'src/app/shared/services/media-mode.service';
import { ReleaseLibraryGroup, ReleaseLibraryItem, ReleaseLibraryKind } from '../models/release-library.model';
import { ReleaseLibraryService } from '../services/release-library.service';

const SEASONS = [
  { name: 'Winter', startMonth: 0 },
  { name: 'Spring', startMonth: 3 },
  { name: 'Summer', startMonth: 6 },
  { name: 'Fall', startMonth: 9 },
] as const;

@Component({
  selector: 'app-release-library',
  templateUrl: './release-library.page.html',
  styleUrls: ['./release-library.page.scss'],
  imports: [
    CommonModule,
    FormsModule,
    IonBadge,
    IonButton,
    IonContent,
    IonIcon,
    IonSegment,
    IonSegmentButton,
    IonSkeletonText,
  ],
})
export class ReleaseLibraryPage implements OnInit {
  selectedKind: ReleaseLibraryKind = 'games';
  games: ReleaseLibraryItem[] = [];
  animes: ReleaseLibraryItem[] = [];
  selectedGameMonth = this.startOfMonth(new Date());
  selectedAnimeSeason = this.startOfSeason(new Date());
  isLoading = true;
  errorMessage = '';

  constructor(
    private readonly releaseLibrary: ReleaseLibraryService,
    private readonly mediaMode: MediaModeService,
  ) {
    addIcons({
      calendarClearOutline,
      filmOutline,
      gameControllerOutline,
      peopleOutline,
      refreshOutline,
      sparklesOutline,
      timeOutline,
    });
  }

  ngOnInit(): void {
    this.load();
  }

  get title(): string {
    return this.selectedKind === 'games' ? 'Game releases' : 'Anime seasons';
  }

  get description(): string {
    return this.selectedKind === 'games'
      ? 'Review one release month at a time, with the most popular games highlighted first.'
      : 'Review one anime season at a time, with the most popular titles highlighted first.';
  }

  get items(): ReleaseLibraryItem[] {
    return this.selectedKind === 'games' ? this.games : this.animes;
  }

  get selectedGroup(): ReleaseLibraryGroup {
    return this.selectedKind === 'games'
      ? this.groupForGameMonth(this.selectedGameMonth)
      : this.groupForAnimeSeason(this.selectedAnimeSeason);
  }

  get topItems(): ReleaseLibraryItem[] {
    return this.selectedGroup.items.slice(0, 4);
  }

  get hasItemsInPeriod(): boolean {
    return this.selectedGroup.items.length > 0;
  }

  get periodLabel(): string {
    return this.selectedGroup.title;
  }

  get periodSubtitle(): string {
    return this.selectedKind === 'games'
      ? 'Monthly release window'
      : 'Seasonal release window';
  }

  selectKind(kind: ReleaseLibraryKind): void {
    this.selectedKind = kind;
    this.mediaMode.select(kind);
  }

  previousPeriod(): void {
    if (this.selectedKind === 'games') {
      this.selectedGameMonth = this.addMonths(this.selectedGameMonth, -1);
      return;
    }

    this.selectedAnimeSeason = this.addMonths(this.selectedAnimeSeason, -3);
  }

  nextPeriod(): void {
    if (this.selectedKind === 'games') {
      this.selectedGameMonth = this.addMonths(this.selectedGameMonth, 1);
      return;
    }

    this.selectedAnimeSeason = this.addMonths(this.selectedAnimeSeason, 3);
  }

  jumpToCurrentPeriod(): void {
    const now = new Date();
    if (this.selectedKind === 'games') {
      this.selectedGameMonth = this.startOfMonth(now);
      return;
    }

    this.selectedAnimeSeason = this.startOfSeason(now);
  }

  load(): void {
    this.isLoading = true;
    this.errorMessage = '';

    forkJoin({
      games: this.releaseLibrary.getGameReleases(),
      animes: this.releaseLibrary.getAnimeReleases(),
    }).subscribe({
      next: ({ games, animes }) => {
        this.games = games ?? [];
        this.animes = animes ?? [];
        this.selectedGameMonth = this.latestMonth(this.games) ?? this.startOfMonth(new Date());
        this.selectedAnimeSeason = this.latestSeason(this.animes) ?? this.startOfSeason(new Date());
        this.isLoading = false;
      },
      error: () => {
        this.games = [];
        this.animes = [];
        this.errorMessage = 'Release library could not be loaded.';
        this.isLoading = false;
      },
    });
  }

  imageFor(item: ReleaseLibraryItem): string {
    return mediaImageUrl(item.image);
  }

  releaseLabel(item: ReleaseLibraryItem): string {
    const date = this.releaseDate(item);
    if (!date) {
      return 'No date';
    }

    return new Intl.DateTimeFormat('en', {
      month: 'short',
      day: 'numeric',
      year: 'numeric',
    }).format(date);
  }

  detailsLabel(item: ReleaseLibraryItem): string {
    if (item.kind === 'games') {
      const platform = Platforms.find((candidate) => candidate.value === Number(item.platforms))?.label ?? 'Unknown';
      const playtime = Number(item.playtime) || 0;
      return playtime > 0 ? `${platform} · ${playtime}h` : platform;
    }

    const episodes = Number(item.episodeCount) || 0;
    const watchTime = this.formatMinutes(item.expectedWatchTimeMinutes);
    if (episodes > 0 && watchTime) {
      return `${episodes} episodes · ${watchTime}`;
    }

    return episodes > 0 ? `${episodes} episodes` : watchTime || 'Anime';
  }

  trackByGroup(_: number, group: ReleaseLibraryGroup): string {
    return group.id;
  }

  trackByItem(_: number, item: ReleaseLibraryItem): string {
    return `${item.kind}-${item.id}`;
  }

  private groupForGameMonth(month: Date): ReleaseLibraryGroup {
    const start = this.startOfMonth(month);
    const end = this.addMonths(start, 1);
    const items = this.games
      .filter((item) => this.isInRange(item, start, end))
      .sort((a, b) => b.addedCount - a.addedCount || this.releaseTime(a) - this.releaseTime(b));

    return {
      id: `${start.getFullYear()}-${start.getMonth()}`,
      title: new Intl.DateTimeFormat('en', { month: 'long', year: 'numeric' }).format(start),
      subtitle: 'Most popular games first',
      sortValue: start.getTime(),
      items,
    };
  }

  private groupForAnimeSeason(seasonStart: Date): ReleaseLibraryGroup {
    const start = this.startOfSeason(seasonStart);
    const end = this.addMonths(start, 3);
    const season = this.seasonFor(start);
    const items = this.animes
      .filter((item) => this.isInRange(item, start, end))
      .sort((a, b) => b.addedCount - a.addedCount || this.releaseTime(a) - this.releaseTime(b));

    return {
      id: `${start.getFullYear()}-${season.name}`,
      title: `${season.name} ${start.getFullYear()}`,
      subtitle: 'Most popular anime first',
      sortValue: start.getTime(),
      items,
    };
  }

  private seasonFor(date: Date): typeof SEASONS[number] {
    const month = date.getMonth();
    if (month >= 9) {
      return SEASONS[3];
    }

    if (month >= 6) {
      return SEASONS[2];
    }

    if (month >= 3) {
      return SEASONS[1];
    }

    return SEASONS[0];
  }

  private releaseTime(item: ReleaseLibraryItem): number {
    return this.releaseDate(item)?.getTime() ?? 0;
  }

  private isInRange(item: ReleaseLibraryItem, start: Date, end: Date): boolean {
    const date = this.releaseDate(item);
    return !!date && date >= start && date < end;
  }

  private latestMonth(items: ReleaseLibraryItem[]): Date | null {
    const latest = this.latestReleaseDate(items);
    return latest ? this.startOfMonth(latest) : null;
  }

  private latestSeason(items: ReleaseLibraryItem[]): Date | null {
    const latest = this.latestReleaseDate(items);
    return latest ? this.startOfSeason(latest) : null;
  }

  private latestReleaseDate(items: ReleaseLibraryItem[]): Date | null {
    return items
      .map((item) => this.releaseDate(item))
      .filter((date): date is Date => !!date)
      .sort((a, b) => b.getTime() - a.getTime())[0] ?? null;
  }

  private startOfMonth(date: Date): Date {
    return new Date(date.getFullYear(), date.getMonth(), 1);
  }

  private startOfSeason(date: Date): Date {
    const season = this.seasonFor(date);
    return new Date(date.getFullYear(), season.startMonth, 1);
  }

  private addMonths(date: Date, months: number): Date {
    return new Date(date.getFullYear(), date.getMonth() + months, 1);
  }

  private releaseDate(item: ReleaseLibraryItem): Date | null {
    if (!item.releaseDate) {
      return null;
    }

    const date = new Date(item.releaseDate);
    return Number.isNaN(date.getTime()) ? null : date;
  }

  private formatMinutes(value: number | null | undefined): string {
    const minutes = Number(value) || 0;
    if (minutes <= 0) {
      return '';
    }

    const hours = Math.floor(minutes / 60);
    const remaining = minutes % 60;
    if (hours > 0 && remaining > 0) {
      return `${hours}h ${remaining}m`;
    }

    return hours > 0 ? `${hours}h` : `${remaining}m`;
  }
}
