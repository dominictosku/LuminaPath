import { Component, Input, OnChanges, SimpleChanges, inject } from '@angular/core';
import { AlertController, IonIcon, IonSpinner } from '@ionic/angular/standalone';
import { firstValueFrom } from 'rxjs';
import {
  MediaVideo,
  MediaVideoContext,
  MediaVideoDraft,
  MediaVideoUpdate,
} from '../../models/media-video.model';
import { MediaVideoService } from '../../services/media-video.service';
import { extractErrorMessage } from '../../utils/extract-error';
import { MediaVideoCardComponent } from './media-video-card/media-video-card.component';
import { MediaVideoUploadComponent } from './media-video-upload/media-video-upload.component';

@Component({
  selector: 'app-media-videos',
  templateUrl: './media-videos.component.html',
  styleUrls: ['./media-videos.component.scss'],
  imports: [IonIcon, IonSpinner, MediaVideoCardComponent, MediaVideoUploadComponent],
})
export class MediaVideosComponent implements OnChanges {
  @Input({ required: true }) mediaId: number | null = null;
  @Input() mediaKind: MediaVideoContext = 'games';
  @Input() canEdit = false;

  private readonly mediaVideoService = inject(MediaVideoService);
  private readonly alertController = inject(AlertController);

  videos: MediaVideo[] = [];
  isLoading = false;
  isUploading = false;
  isMutating = false;
  errorMessage = '';
  uploadMessage = '';
  uploadResetKey = 0;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['mediaId']) {
      void this.loadVideos();
    }
  }

  get headerTitle(): string {
    const count = this.videos.length;
    return count === 1 ? '1 video' : `${count} videos`;
  }

  async loadVideos(): Promise<void> {
    const mediaId = this.mediaId;
    if (mediaId == null) {
      this.videos = [];
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';
    try {
      this.videos = await firstValueFrom(this.mediaVideoService.list(mediaId));
    } catch (error) {
      this.errorMessage = extractErrorMessage(error, 'Videos could not be loaded.');
    } finally {
      this.isLoading = false;
    }
  }

  async uploadVideo(draft: MediaVideoDraft): Promise<void> {
    const mediaId = this.mediaId;
    if (!this.canEdit || this.isUploading || mediaId == null) {
      return;
    }

    this.isUploading = true;
    this.errorMessage = '';
    this.uploadMessage = '';
    try {
      await firstValueFrom(this.mediaVideoService.upload({
        mediaId,
        file: draft.file,
        title: draft.title,
        description: draft.description,
        kind: draft.kind,
      }));
      this.uploadMessage = 'Video uploaded.';
      this.uploadResetKey++;
      await this.loadVideos();
    } catch (error) {
      this.uploadMessage = extractErrorMessage(error, 'Video could not be uploaded.');
    } finally {
      this.isUploading = false;
    }
  }

  async saveVideo(video: MediaVideo, update: MediaVideoUpdate): Promise<void> {
    if (!this.canEdit || this.isMutating || !update.title.trim()) {
      return;
    }

    this.isMutating = true;
    this.errorMessage = '';
    try {
      const updated = await firstValueFrom(this.mediaVideoService.update(video.id, update));
      this.videos = this.videos.map((item) => item.id === video.id ? updated : item)
        .sort((a, b) => a.sortOrder - b.sortOrder || a.id - b.id);
    } catch (error) {
      this.errorMessage = extractErrorMessage(error, 'Video could not be saved.');
    } finally {
      this.isMutating = false;
    }
  }

  async moveVideo(video: MediaVideo, direction: -1 | 1): Promise<void> {
    const mediaId = this.mediaId;
    if (!this.canEdit || this.isMutating || mediaId == null) {
      return;
    }

    const currentIndex = this.videos.findIndex((item) => item.id === video.id);
    const nextIndex = currentIndex + direction;
    if (currentIndex < 0 || nextIndex < 0 || nextIndex >= this.videos.length) {
      return;
    }

    const nextVideos = [...this.videos];
    [nextVideos[currentIndex], nextVideos[nextIndex]] = [nextVideos[nextIndex], nextVideos[currentIndex]];

    this.isMutating = true;
    this.errorMessage = '';
    try {
      this.videos = await firstValueFrom(
        this.mediaVideoService.reorder(mediaId, nextVideos.map((item) => item.id)),
      );
    } catch (error) {
      this.errorMessage = extractErrorMessage(error, 'Video order could not be saved.');
    } finally {
      this.isMutating = false;
    }
  }

  async confirmDelete(video: MediaVideo): Promise<void> {
    if (!this.canEdit || this.isMutating) {
      return;
    }

    const alert = await this.alertController.create({
      header: `Delete "${video.title}"?`,
      message: 'This removes the uploaded video from this media item.',
      cssClass: 'media-confirm-alert',
      buttons: [
        { text: 'Keep', role: 'cancel' },
        {
          text: 'Delete',
          role: 'destructive',
          cssClass: 'media-confirm-alert__destructive',
          handler: () => {
            void this.deleteVideo(video);
          },
        },
      ],
    });
    await alert.present();
  }

  streamUrl(video: MediaVideo): string {
    return this.mediaVideoService.streamUrl(video);
  }

  private async deleteVideo(video: MediaVideo): Promise<void> {
    this.isMutating = true;
    this.errorMessage = '';
    try {
      await firstValueFrom(this.mediaVideoService.delete(video.id));
      this.videos = this.videos.filter((item) => item.id !== video.id);
    } catch (error) {
      this.errorMessage = extractErrorMessage(error, 'Video could not be deleted.');
    } finally {
      this.isMutating = false;
    }
  }
}
