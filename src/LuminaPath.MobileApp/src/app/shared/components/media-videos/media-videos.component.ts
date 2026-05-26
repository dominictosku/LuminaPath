import { Component, ElementRef, Input, OnChanges, SimpleChanges, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { AlertController, IonBadge, IonButton, IonIcon, IonSpinner } from '@ionic/angular/standalone';
import { firstValueFrom } from 'rxjs';
import {
  MediaVideo,
  MediaVideoContext,
  MediaVideoKind,
} from '../../models/media-video.model';
import { MediaVideoService } from '../../services/media-video.service';
import { extractErrorMessage } from '../../utils/extract-error';

interface VideoKindOption {
  label: string;
  value: MediaVideoKind;
}

@Component({
  selector: 'app-media-videos',
  templateUrl: './media-videos.component.html',
  styleUrls: ['./media-videos.component.scss'],
  imports: [FormsModule, IonBadge, IonButton, IonIcon, IonSpinner],
})
export class MediaVideosComponent implements OnChanges {
  @Input({ required: true }) mediaId: number | null = null;
  @Input() mediaKind: MediaVideoContext = 'games';
  @Input() canEdit = false;

  @ViewChild('videoFileInput') private readonly fileInput?: ElementRef<HTMLInputElement>;

  private readonly mediaVideoService = inject(MediaVideoService);
  private readonly alertController = inject(AlertController);

  readonly MediaVideoKind = MediaVideoKind;

  videos: MediaVideo[] = [];
  isLoading = false;
  isUploading = false;
  isMutating = false;
  errorMessage = '';
  uploadMessage = '';
  uploadTitle = '';
  uploadDescription = '';
  uploadKind: MediaVideoKind = MediaVideoKind.Clip;
  selectedFile: File | null = null;
  editingVideoId: number | null = null;
  editTitle = '';
  editDescription = '';
  editKind: MediaVideoKind = MediaVideoKind.Clip;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['mediaKind']) {
      this.uploadKind = this.defaultKind;
    }

    if (changes['mediaId']) {
      void this.loadVideos();
    }
  }

  get kindOptions(): VideoKindOption[] {
    if (this.mediaKind === 'games') {
      return [
        { label: 'Clip', value: MediaVideoKind.Clip },
        { label: 'Guide', value: MediaVideoKind.Guide },
      ];
    }

    return [
      { label: 'Scene clip', value: MediaVideoKind.SceneClip },
    ];
  }

  get headerTitle(): string {
    const count = this.videos.length;
    return count === 1 ? '1 video' : `${count} videos`;
  }

  get uploadDisabled(): boolean {
    return !this.canEdit
      || this.isUploading
      || this.mediaId == null
      || this.selectedFile == null;
  }

  get selectedFileName(): string {
    return this.selectedFile?.name ?? 'No file selected';
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement | null;
    this.selectedFile = input?.files?.[0] ?? null;
    this.uploadMessage = '';

    if (!this.uploadTitle.trim() && this.selectedFile) {
      this.uploadTitle = this.selectedFile.name.replace(/\.[^.]+$/, '');
    }
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

  async uploadVideo(event?: Event): Promise<void> {
    event?.preventDefault();
    const mediaId = this.mediaId;
    const file = this.selectedFile;
    if (this.uploadDisabled || mediaId == null || !file) {
      return;
    }

    this.isUploading = true;
    this.errorMessage = '';
    this.uploadMessage = '';
    try {
      await firstValueFrom(this.mediaVideoService.upload({
        mediaId,
        file,
        title: this.uploadTitle.trim(),
        description: this.uploadDescription.trim(),
        kind: this.uploadKind,
      }));
      this.uploadMessage = 'Video uploaded.';
      this.resetUploadForm();
      await this.loadVideos();
    } catch (error) {
      this.uploadMessage = extractErrorMessage(error, 'Video could not be uploaded.');
    } finally {
      this.isUploading = false;
    }
  }

  startEdit(video: MediaVideo): void {
    if (!this.canEdit || this.isMutating) {
      return;
    }

    this.editingVideoId = video.id;
    this.editTitle = video.title;
    this.editDescription = video.description ?? '';
    this.editKind = video.kind;
  }

  cancelEdit(): void {
    this.editingVideoId = null;
    this.editTitle = '';
    this.editDescription = '';
    this.editKind = this.defaultKind;
  }

  async saveEdit(video: MediaVideo): Promise<void> {
    if (!this.canEdit || this.isMutating || !this.editTitle.trim()) {
      return;
    }

    this.isMutating = true;
    this.errorMessage = '';
    try {
      const updated = await firstValueFrom(this.mediaVideoService.update(video.id, {
        title: this.editTitle.trim(),
        description: this.editDescription.trim() || null,
        kind: this.editKind,
      }));
      this.videos = this.videos.map((item) => item.id === video.id ? updated : item)
        .sort((a, b) => a.sortOrder - b.sortOrder || a.id - b.id);
      this.cancelEdit();
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

  kindLabel(kind: MediaVideoKind): string {
    return this.kindOptions.find((option) => option.value === kind)?.label
      ?? (kind === MediaVideoKind.Guide ? 'Guide' : kind === MediaVideoKind.SceneClip ? 'Scene clip' : 'Clip');
  }

  formatFileSize(sizeBytes: number): string {
    if (!Number.isFinite(sizeBytes) || sizeBytes <= 0) {
      return 'Unknown size';
    }

    const megabytes = sizeBytes / 1024 / 1024;
    if (megabytes >= 1) {
      return `${Math.round(megabytes * 10) / 10} MB`;
    }

    return `${Math.max(1, Math.round(sizeBytes / 1024))} KB`;
  }

  private async deleteVideo(video: MediaVideo): Promise<void> {
    this.isMutating = true;
    this.errorMessage = '';
    try {
      await firstValueFrom(this.mediaVideoService.delete(video.id));
      this.videos = this.videos.filter((item) => item.id !== video.id);
      this.cancelEdit();
    } catch (error) {
      this.errorMessage = extractErrorMessage(error, 'Video could not be deleted.');
    } finally {
      this.isMutating = false;
    }
  }

  private resetUploadForm(): void {
    this.uploadTitle = '';
    this.uploadDescription = '';
    this.uploadKind = this.defaultKind;
    this.selectedFile = null;
    if (this.fileInput?.nativeElement) {
      this.fileInput.nativeElement.value = '';
    }
  }

  private get defaultKind(): MediaVideoKind {
    return this.mediaKind === 'games' ? MediaVideoKind.Clip : MediaVideoKind.SceneClip;
  }
}
