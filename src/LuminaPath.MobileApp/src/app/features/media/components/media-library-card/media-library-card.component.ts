import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { IonBadge, IonButton, IonIcon } from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  addOutline,
  calendarClearOutline,
  createOutline,
  hourglassOutline,
  timeOutline,
} from 'ionicons/icons';
import { MediaModeOption } from 'src/app/shared/services/media-mode.service';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { MediaItem } from '../../models/media-item.model';
import { MediaLibraryViewService } from '../../services/media-library-view.service';

@Component({
  selector: 'app-media-library-card',
  templateUrl: './media-library-card.component.html',
  styleUrls: ['./media-library-card.component.scss'],
  imports: [CommonModule, IonBadge, IonButton, IonIcon],
})
export class MediaLibraryCardComponent {
  @Input({ required: true }) item!: MediaItem;
  @Input({ required: true }) mediaMode!: MediaModeOption;
  @Input() isAdding = false;

  @Output() edit = new EventEmitter<MediaItem>();
  @Output() details = new EventEmitter<MediaItem>();

  constructor(public readonly view: MediaLibraryViewService) {
    addIcons({
      addOutline,
      calendarClearOutline,
      createOutline,
      hourglassOutline,
      timeOutline,
    });
  }

  imageFor(item: MediaItem): string {
    return mediaImageUrl(item.image);
  }
}
