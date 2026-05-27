import { Component, ElementRef, EventEmitter, Input, OnChanges, Output, SimpleChanges, ViewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IonButton, IonIcon } from '@ionic/angular/standalone';
import {
  MediaVideoContext,
  MediaVideoDraft,
  MediaVideoKind,
  defaultMediaVideoKind,
  mediaVideoKindOptions,
} from '../../../models/media-video.model';

@Component({
  selector: 'app-media-video-upload',
  templateUrl: './media-video-upload.component.html',
  styleUrls: ['./media-video-upload.component.scss'],
  imports: [FormsModule, IonButton, IonIcon],
})
export class MediaVideoUploadComponent implements OnChanges {
  @Input() mediaKind: MediaVideoContext = 'games';
  @Input() disabled = false;
  @Input() isUploading = false;
  @Input() message = '';
  @Input() resetKey = 0;

  @Output() upload = new EventEmitter<MediaVideoDraft>();

  @ViewChild('videoFileInput') private readonly fileInput?: ElementRef<HTMLInputElement>;

  uploadTitle = '';
  uploadDescription = '';
  uploadKind: MediaVideoKind = MediaVideoKind.Clip;
  selectedFile: File | null = null;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['mediaKind'] && !changes['mediaKind'].firstChange) {
      this.uploadKind = this.defaultKind;
    }

    if (changes['resetKey'] && !changes['resetKey'].firstChange) {
      this.reset();
    }
  }

  get kindOptions() {
    return mediaVideoKindOptions(this.mediaKind);
  }

  get uploadDisabled(): boolean {
    return this.disabled || this.isUploading || this.selectedFile == null;
  }

  get selectedFileName(): string {
    return this.selectedFile?.name ?? 'No file selected';
  }

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement | null;
    this.selectedFile = input?.files?.[0] ?? null;

    if (!this.uploadTitle.trim() && this.selectedFile) {
      this.uploadTitle = this.selectedFile.name.replace(/\.[^.]+$/, '');
    }
  }

  submit(event: Event): void {
    event.preventDefault();
    const file = this.selectedFile;
    if (this.uploadDisabled || !file) {
      return;
    }

    this.upload.emit({
      file,
      title: this.uploadTitle.trim(),
      description: this.uploadDescription.trim(),
      kind: this.uploadKind,
    });
  }

  private reset(): void {
    this.uploadTitle = '';
    this.uploadDescription = '';
    this.uploadKind = this.defaultKind;
    this.selectedFile = null;
    if (this.fileInput?.nativeElement) {
      this.fileInput.nativeElement.value = '';
    }
  }

  private get defaultKind(): MediaVideoKind {
    return defaultMediaVideoKind(this.mediaKind);
  }
}
