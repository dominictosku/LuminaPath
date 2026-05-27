import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { IonBadge, IonButton, IonIcon } from '@ionic/angular/standalone';
import {
  MediaVideo,
  MediaVideoContext,
  MediaVideoKind,
  MediaVideoUpdate,
  defaultMediaVideoKind,
  formatMediaVideoFileSize,
  mediaVideoKindLabel,
  mediaVideoKindOptions,
} from '../../../models/media-video.model';

@Component({
  selector: 'app-media-video-card',
  templateUrl: './media-video-card.component.html',
  styleUrls: ['./media-video-card.component.scss'],
  imports: [FormsModule, IonBadge, IonButton, IonIcon],
})
export class MediaVideoCardComponent implements OnChanges {
  @Input({ required: true }) video!: MediaVideo;
  @Input({ required: true }) streamUrl = '';
  @Input() mediaKind: MediaVideoContext = 'games';
  @Input() canEdit = false;
  @Input() isFirst = false;
  @Input() isLast = false;
  @Input() isMutating = false;

  @Output() move = new EventEmitter<-1 | 1>();
  @Output() save = new EventEmitter<MediaVideoUpdate>();
  @Output() delete = new EventEmitter<MediaVideo>();

  isEditing = false;
  editTitle = '';
  editDescription = '';
  editKind: MediaVideoKind = MediaVideoKind.Clip;

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['video'] && !this.isEditing) {
      this.resetEditForm();
    }

    if (changes['mediaKind'] && !this.isEditing) {
      this.editKind = defaultMediaVideoKind(this.mediaKind);
    }
  }

  get kindOptions() {
    return mediaVideoKindOptions(this.mediaKind);
  }

  get kindLabel(): string {
    return mediaVideoKindLabel(this.video.kind, this.mediaKind);
  }

  get fileSizeLabel(): string {
    return formatMediaVideoFileSize(this.video.sizeBytes);
  }

  startEdit(): void {
    if (!this.canEdit || this.isMutating) {
      return;
    }

    this.resetEditForm();
    this.isEditing = true;
  }

  cancelEdit(): void {
    this.isEditing = false;
    this.resetEditForm();
  }

  saveEdit(): void {
    if (!this.canEdit || this.isMutating || !this.editTitle.trim()) {
      return;
    }

    this.save.emit({
      title: this.editTitle.trim(),
      description: this.editDescription.trim() || null,
      kind: this.editKind,
    });
    this.isEditing = false;
  }

  private resetEditForm(): void {
    this.editTitle = this.video?.title ?? '';
    this.editDescription = this.video?.description ?? '';
    this.editKind = this.video?.kind ?? defaultMediaVideoKind(this.mediaKind);
  }
}
