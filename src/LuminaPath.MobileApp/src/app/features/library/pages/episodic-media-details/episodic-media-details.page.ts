import { Location } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
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
  filmOutline,
  layersOutline,
  libraryOutline,
  playForwardOutline,
  refreshOutline,
  returnUpBackOutline,
  sparklesOutline,
  timeOutline,
  trashOutline,
  tvOutline,
} from 'ionicons/icons';
import { firstValueFrom } from 'rxjs';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { formatHoursMinutes, formatShortDate } from 'src/app/shared/utils/format';
import { LibraryEntryDetails } from '../../models/media-item.model';
import { MediaStore } from '../../state/media.store';
import { EPISODIC_MEDIA_ADAPTER, EPISODIC_MEDIA_CONFIG } from './episodic-media.tokens';
import {
  EpisodicLibraryEntry,
  EpisodicMediaSummary,
  EpisodicMediaView,
} from './episodic-media.types';

const WATCH_STATUS_LABELS: Record<number, string> = {
  0: 'On hold',
  1: 'Planned',
  2: 'Watching',
  3: 'Completed',
  4: 'Dropped',
};

@Component({
  selector: 'app-episodic-media-details',
  templateUrl: './episodic-media-details.page.html',
  styleUrls: ['./episodic-media-details.page.scss'],
  imports: [
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
    IonToolbar
],
})
export class EpisodicMediaDetailsPage implements OnInit {
  readonly config = inject(EPISODIC_MEDIA_CONFIG);
  private readonly adapter = inject(EPISODIC_MEDIA_ADAPTER);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly location = inject(Location);
  private readonly alertController = inject(AlertController);
  private readonly actionSheetController = inject(ActionSheetController);
  private readonly mediaStore = inject(MediaStore);

  media: EpisodicMediaView | null = null;
  isLoading = true;
  errorMessage = '';
  isUpdatingLibrary = false;
  headerCondensed = false;

  constructor() {
    addIcons({
      addOutline,
      arrowBackOutline,
      calendarClearOutline,
      checkmarkDoneOutline,
      ellipsisVertical,
      filmOutline,
      layersOutline,
      libraryOutline,
      playForwardOutline,
      refreshOutline,
      returnUpBackOutline,
      sparklesOutline,
      timeOutline,
      trashOutline,
      tvOutline,
    });
  }

  ngOnInit(): void {
    this.route.paramMap.subscribe(async (params) => {
      const mediaId = Number(params.get(this.config.paramKey));
      if (!Number.isInteger(mediaId) || mediaId <= 0) {
        this.showError(this.config.notFoundLabel);
        return;
      }

      this.isLoading = true;
      this.errorMessage = '';
      await this.loadMedia(mediaId);
    });
  }

  get libraryEntry(): EpisodicLibraryEntry | null {
    return this.media?.libraryEntry ?? null;
  }

  get libraryEntryId(): number | null {
    const id = Number(this.libraryEntry?.id);
    return Number.isInteger(id) && id > 0 ? id : null;
  }

  get isInLibrary(): boolean {
    return this.libraryEntryId !== null;
  }

  get libraryStatusLabel(): string {
    return this.isInLibrary ? 'Already in your library' : 'Not in library';
  }

  get statusLabel(): string {
    const status = this.libraryEntry?.status;
    return status == null ? 'Catalog' : WATCH_STATUS_LABELS[Number(status)] ?? 'Catalog';
  }

  get releaseLabel(): string {
    return formatShortDate(this.media?.releaseDate);
  }

  get fourthMetaLabel(): string {
    const value = this.media?.[this.config.fourthMetaSource] ?? null;
    const formatted = formatHoursMinutes(value);
    if (this.config.perEpisodeSuffix && value != null && Number(value) > 0) {
      return `${formatted} ${this.config.perEpisodeSuffix}`;
    }
    return formatted;
  }

  get genreLabel(): string {
    const genre = (this.media?.genre ?? '').trim();
    return genre.length ? genre : 'Unspecified';
  }

  get currentEpisode(): number {
    return Math.max(0, Number(this.libraryEntry?.currentEpisode) || 0);
  }

  get totalEpisodes(): number {
    return Math.max(0, Number(this.media?.episodeCount) || 0);
  }

  get episodeLabel(): string {
    if (this.totalEpisodes <= 0) {
      return this.currentEpisode > 0 ? `Episode ${this.currentEpisode}` : 'No episode count';
    }
    return this.isInLibrary
      ? `Episode ${this.currentEpisode}/${this.totalEpisodes}`
      : `${this.totalEpisodes} episodes`;
  }

  get progress(): number {
    const total = this.totalEpisodes;
    return total > 0 ? Math.min(100, Math.round((this.currentEpisode / total) * 100)) : 0;
  }

  get seasons(): EpisodicMediaSummary[] {
    return this.media?.seasons ?? [];
  }

  get hasParent(): boolean {
    return !!this.media?.parentId;
  }

  get primaryActionLabel(): string {
    const status = Number(this.libraryEntry?.status ?? 1);
    if (status === 3) return this.config.replayLabel;
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
    return mediaImageUrl(this.media?.image);
  }

  seasonImageUrl(season: EpisodicMediaSummary): string {
    return mediaImageUrl(season.image ?? null);
  }

  seasonReleaseLabel(season: EpisodicMediaSummary): string {
    return formatShortDate(season.releaseDate);
  }

  onScroll(event: CustomEvent<{ scrollTop: number }>): void {
    const scrollTop = event.detail?.scrollTop ?? 0;
    const condensed = scrollTop > 140;
    if (condensed !== this.headerCondensed) {
      this.headerCondensed = condensed;
    }
  }

  async addToLibrary(): Promise<void> {
    const mediaId = this.media?.id;
    if (!mediaId || this.isUpdatingLibrary) return;
    this.isUpdatingLibrary = true;
    try {
      await firstValueFrom(
        this.adapter.add(mediaId, {
          status: 1,
          timeSpend: null,
          rating: null,
          startDate: null,
          endDate: null,
          currentEpisode: 0,
        }),
      );
      await this.loadMedia(mediaId);
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
      header: this.media?.name ?? this.config.optionsHeaderFallback,
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
      subHeader: this.media?.name ?? undefined,
      message: this.config.removeMessage,
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

  goBack(): void {
    if (window.history.length > 1) {
      this.location.back();
      return;
    }

    void this.router.navigateByUrl('/library');
  }

  private async removeFromLibrary(): Promise<void> {
    const mediaId = this.media?.id;
    const libraryEntryId = this.libraryEntryId;
    if (!mediaId || libraryEntryId == null || this.isUpdatingLibrary) return;

    this.isUpdatingLibrary = true;
    try {
      await firstValueFrom(this.adapter.delete(libraryEntryId));
      await this.loadMedia(mediaId);
    } catch {
      // silent
    } finally {
      this.isUpdatingLibrary = false;
    }
  }

  private async updateLibrary(patch: Partial<LibraryEntryDetails>): Promise<void> {
    const mediaId = this.media?.id;
    const libraryEntryId = this.libraryEntryId;
    const entry = this.libraryEntry;
    if (!mediaId || libraryEntryId == null || entry == null || this.isUpdatingLibrary) return;

    this.isUpdatingLibrary = true;
    try {
      await firstValueFrom(
        this.adapter.update(libraryEntryId, mediaId, {
          status: patch.status ?? Number(entry.status ?? 1),
          timeSpend: patch.timeSpend ?? entry.timeSpend ?? null,
          rating: patch.rating ?? entry.rating ?? null,
          startDate: patch.startDate ?? this.toIsoString(entry.startDate),
          endDate: patch.endDate ?? this.toIsoString(entry.endDate),
          currentEpisode: patch.currentEpisode ?? Number(entry.currentEpisode ?? 0),
        }),
      );
      await this.loadMedia(mediaId);
    } catch {
      // silent
    } finally {
      this.isUpdatingLibrary = false;
    }
  }

  private async loadMedia(mediaId: number): Promise<void> {
    try {
      this.media = await firstValueFrom(this.adapter.load(mediaId));
      this.syncMediaStore(mediaId);
    } catch {
      this.showError(this.config.loadErrorLabel);
    } finally {
      this.isLoading = false;
    }
  }

  private syncMediaStore(mediaId: number): void {
    const entry = this.libraryEntry;
    this.mediaStore.setLibraryEntry(mediaId, entry ? {
      id: entry.id,
      rating: entry.rating,
      startDate: entry.startDate,
      endDate: entry.endDate,
      status: Number(entry.status ?? 1),
      timeSpend: entry.timeSpend,
      currentEpisode: entry.currentEpisode,
    } : null);
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
