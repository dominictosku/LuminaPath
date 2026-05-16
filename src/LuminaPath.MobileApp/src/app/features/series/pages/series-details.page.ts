import { CommonModule, Location } from '@angular/common';
import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import {
  ActionSheetController,
  AlertController,
  IonBadge,
  IonButton,
  IonButtons,
  IonContent,
  IonHeader,
  IonIcon,
  IonProgressBar,
  IonSpinner,
  IonTitle,
  IonToolbar,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  addOutline,
  arrowBackOutline,
  calendarClearOutline,
  checkmarkDoneOutline,
  ellipsisVertical,
  layersOutline,
  libraryOutline,
  playForwardOutline,
  refreshOutline,
  returnUpBackOutline,
  timeOutline,
  trashOutline,
  tvOutline,
} from 'ionicons/icons';
import { firstValueFrom } from 'rxjs';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { MySeries, Series, SeriesSummary } from '../models/series.model';
import { SeriesService } from '../services/series.service';
import { MySeriesService } from '../services/my-series.service';
import { LibraryEntryDetails } from '../../library/models/media-item.model';

const WATCH_STATUS_LABELS: Record<number, string> = {
  0: 'On hold',
  1: 'Planned',
  2: 'Watching',
  3: 'Completed',
  4: 'Dropped',
};

@Component({
  selector: 'app-series-details',
  templateUrl: './series-details.page.html',
  styleUrls: ['./series-details.page.scss'],
  imports: [
    CommonModule,
    RouterLink,
    IonBadge,
    IonButton,
    IonButtons,
    IonContent,
    IonHeader,
    IonIcon,
    IonProgressBar,
    IonSpinner,
    IonTitle,
    IonToolbar,
  ],
})
export class SeriesDetailsPage implements OnInit {
  series: Series | null = null;
  isLoading = true;
  errorMessage = '';

  isUpdatingLibrary = false;
  headerCondensed = false;

  constructor(
    private readonly route: ActivatedRoute,
    private readonly router: Router,
    private readonly location: Location,
    private readonly seriesService: SeriesService,
    private readonly mySeriesService: MySeriesService,
    private readonly alertController: AlertController,
    private readonly actionSheetController: ActionSheetController,
  ) {
    addIcons({
      addOutline,
      arrowBackOutline,
      calendarClearOutline,
      checkmarkDoneOutline,
      ellipsisVertical,
      layersOutline,
      libraryOutline,
      playForwardOutline,
      refreshOutline,
      returnUpBackOutline,
      timeOutline,
      trashOutline,
      tvOutline,
    });
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(async (params) => {
      const seriesId = Number(params.get('seriesId'));
      if (!Number.isInteger(seriesId) || seriesId <= 0) {
        this.showError('Series not found.');
        return;
      }

      this.isLoading = true;
      this.errorMessage = '';
      await this.loadSeries(seriesId);
    });
  }

  get libraryEntry(): MySeries | null {
    return this.series?.mySeries ?? null;
  }

  get mySeriesId(): number | null {
    const id = Number(this.libraryEntry?.id);
    return Number.isInteger(id) && id > 0 ? id : null;
  }

  get isInLibrary(): boolean {
    return this.mySeriesId !== null;
  }

  get libraryStatusLabel(): string {
    return this.isInLibrary ? 'Already in your library' : 'Not in library';
  }

  get statusLabel(): string {
    const status = this.libraryEntry?.status;
    return status == null ? 'Catalog' : WATCH_STATUS_LABELS[Number(status)] ?? 'Catalog';
  }

  get releaseLabel(): string {
    return this.formatDate(this.series?.releaseDate);
  }

  get watchTimeLabel(): string {
    return this.formatMinutes(this.series?.expectedWatchTimeMinutes);
  }

  get episodeTimeLabel(): string {
    const value = this.series?.expectedWatchTimePerEpisodeMinutes;
    return value == null ? 'No estimate' : `${this.formatMinutes(value)} / ep`;
  }

  get genreLabel(): string {
    const genre = (this.series?.genre ?? '').trim();
    return genre.length ? genre : 'Unspecified';
  }

  get currentEpisode(): number {
    return Math.max(0, Number(this.libraryEntry?.currentEpisode) || 0);
  }

  get totalEpisodes(): number {
    return Math.max(0, Number(this.series?.episodeCount) || 0);
  }

  get episodeLabel(): string {
    if (this.totalEpisodes <= 0) {
      return this.currentEpisode > 0 ? `Episode ${this.currentEpisode}` : 'No episode count';
    }
    return this.isInLibrary ? `Episode ${this.currentEpisode}/${this.totalEpisodes}` : `${this.totalEpisodes} episodes`;
  }

  get progress(): number {
    const total = this.totalEpisodes;
    return total > 0 ? Math.min(100, Math.round((this.currentEpisode / total) * 100)) : 0;
  }

  get seasons(): SeriesSummary[] {
    return this.series?.seasons ?? [];
  }

  get hasParent(): boolean {
    return !!this.series?.parentSeriesId;
  }

  get primaryActionLabel(): string {
    const status = Number(this.libraryEntry?.status ?? 1);
    if (status === 3) return 'Rewatch';
    if (this.totalEpisodes > 0 && this.currentEpisode >= this.totalEpisodes) {
      return 'Mark completed';
    }
    if (this.totalEpisodes > 0) {
      return `Watch ep. ${this.currentEpisode + 1}/${this.totalEpisodes}`;
    }
    return `Watch episode ${this.currentEpisode + 1}`;
  }

  get primaryActionIcon(): string {
    const status = Number(this.libraryEntry?.status ?? 1);
    if (status === 3) return 'refresh-outline';
    if (this.totalEpisodes > 0 && this.currentEpisode >= this.totalEpisodes) {
      return 'checkmark-done-outline';
    }
    return 'play-forward-outline';
  }

  get primaryActionDisabled(): boolean {
    return false;
  }

  get canMarkComplete(): boolean {
    const status = Number(this.libraryEntry?.status ?? 1);
    if (status === 3) return false;
    if (this.totalEpisodes > 0 && this.currentEpisode >= this.totalEpisodes) return false;
    return true;
  }

  imageUrl(): string {
    return mediaImageUrl(this.series?.image);
  }

  seasonImageUrl(season: SeriesSummary): string {
    return mediaImageUrl(season.image ?? null);
  }

  seasonReleaseLabel(season: SeriesSummary): string {
    return this.formatDate(season.releaseDate);
  }

  trackBySeason(_: number, season: SeriesSummary): number {
    return season.id;
  }

  onScroll(event: CustomEvent<{ scrollTop: number }>): void {
    const scrollTop = event.detail?.scrollTop ?? 0;
    const condensed = scrollTop > 140;
    if (condensed !== this.headerCondensed) {
      this.headerCondensed = condensed;
    }
  }

  async addToLibrary(): Promise<void> {
    if (!this.series?.id || this.isUpdatingLibrary) return;
    this.isUpdatingLibrary = true;
    try {
      await firstValueFrom(
        this.mySeriesService.addToLibrary(this.series.id, {
          status: 1,
          timeSpend: null,
          rating: null,
          startDate: null,
          endDate: null,
          currentEpisode: 0,
        }),
      );
      await this.loadSeries(this.series.id);
    } catch {
      // silent
    } finally {
      this.isUpdatingLibrary = false;
    }
  }

  async runPrimaryAction(): Promise<void> {
    const status = Number(this.libraryEntry?.status ?? 1);
    if (status === 3) {
      await this.updateLibrary({ currentEpisode: 0, status: 2 });
      return;
    }
    if (this.totalEpisodes > 0 && this.currentEpisode >= this.totalEpisodes) {
      await this.markComplete();
      return;
    }
    await this.watchNextEpisode();
  }

  async watchNextEpisode(): Promise<void> {
    if (this.isUpdatingLibrary) return;
    const next = this.currentEpisode + 1;
    const total = this.totalEpisodes;
    const reachedEnd = total > 0 && next >= total;
    const status = reachedEnd ? 3 : Math.max(2, Number(this.libraryEntry?.status ?? 1));
    await this.updateLibrary({
      currentEpisode: next,
      status,
      endDate: reachedEnd && !this.libraryEntry?.endDate ? new Date().toISOString() : undefined,
    });
  }

  async markComplete(): Promise<void> {
    if (this.isUpdatingLibrary) return;
    const total = this.totalEpisodes;
    await this.updateLibrary({
      currentEpisode: total > 0 ? total : this.currentEpisode,
      status: 3,
      endDate: this.libraryEntry?.endDate ? undefined : new Date().toISOString(),
    });
  }

  async openMoreMenu(): Promise<void> {
    if (!this.isInLibrary) return;

    const sheet = await this.actionSheetController.create({
      header: this.series?.name ?? 'Series options',
      cssClass: 'media-action-sheet',
      buttons: [
        {
          text: 'Remove from library',
          role: 'destructive',
          icon: 'trash-outline',
          handler: () => {
            void this.confirmRemoveFromLibrary();
          },
        },
        {
          text: 'Cancel',
          role: 'cancel',
        },
      ],
    });
    await sheet.present();
  }

  async confirmRemoveFromLibrary(): Promise<void> {
    if (!this.isInLibrary || this.isUpdatingLibrary) return;

    const alert = await this.alertController.create({
      header: 'Remove from library?',
      subHeader: this.series?.name ?? undefined,
      message: 'Your watch progress and dates for this series will be permanently deleted.',
      cssClass: 'media-confirm-alert',
      buttons: [
        { text: 'Keep', role: 'cancel' },
        {
          text: 'Remove',
          role: 'destructive',
          cssClass: 'media-confirm-alert__destructive',
          handler: () => {
            void this.removeFromLibrary();
          },
        },
      ],
    });
    await alert.present();
  }

  private async removeFromLibrary(): Promise<void> {
    const seriesId = this.series?.id;
    const mySeriesId = this.mySeriesId;
    if (!seriesId || mySeriesId == null || this.isUpdatingLibrary) return;

    this.isUpdatingLibrary = true;
    try {
      await firstValueFrom(this.mySeriesService.delete(mySeriesId));
      await this.loadSeries(seriesId);
    } catch {
      // silent
    } finally {
      this.isUpdatingLibrary = false;
    }
  }

  private async updateLibrary(patch: Partial<LibraryEntryDetails>): Promise<void> {
    const seriesId = this.series?.id;
    const mySeriesId = this.mySeriesId;
    const entry = this.libraryEntry;
    if (!seriesId || mySeriesId == null || entry == null || this.isUpdatingLibrary) return;

    this.isUpdatingLibrary = true;
    try {
      await firstValueFrom(
        this.mySeriesService.updateLibraryEntry(mySeriesId, seriesId, {
          status: patch.status ?? Number(entry.status ?? 1),
          timeSpend: patch.timeSpend ?? entry.timeSpend ?? null,
          rating: patch.rating ?? entry.rating ?? null,
          startDate: patch.startDate ?? this.toIsoString(entry.startDate),
          endDate: patch.endDate ?? this.toIsoString(entry.endDate),
          currentEpisode: patch.currentEpisode ?? Number(entry.currentEpisode ?? 0),
        }),
      );
      await this.loadSeries(seriesId);
    } catch {
      // silent
    } finally {
      this.isUpdatingLibrary = false;
    }
  }

  goBack(): void {
    if (window.history.length > 1) {
      this.location.back();
      return;
    }

    void this.router.navigateByUrl('/library');
  }

  private async loadSeries(seriesId: number): Promise<void> {
    try {
      this.series = await firstValueFrom(this.seriesService.get(seriesId));
    } catch {
      this.showError('Series could not be loaded.');
    } finally {
      this.isLoading = false;
    }
  }

  private formatDate(value: Date | string | null | undefined): string {
    if (!value) {
      return 'No release date';
    }

    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return 'No release date';
    }

    return new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric', year: 'numeric' }).format(date);
  }

  private formatMinutes(value: number | null | undefined): string {
    const minutes = Number(value) || 0;
    if (minutes <= 0) {
      return 'No estimate';
    }

    const hours = Math.floor(minutes / 60);
    const remaining = minutes % 60;
    if (hours > 0 && remaining > 0) {
      return `${hours}h ${remaining}m`;
    }
    return hours > 0 ? `${hours}h` : `${remaining}m`;
  }

  private toIsoString(value: Date | string | null | undefined): string | null {
    if (!value) return null;
    if (value instanceof Date) {
      return Number.isNaN(value.getTime()) ? null : value.toISOString();
    }
    return value;
  }

  private showError(message: string): void {
    this.errorMessage = message;
    this.isLoading = false;
  }
}
