
import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { IonBadge, IonButton, IonIcon } from '@ionic/angular/standalone';
import { MediaModeOption } from 'src/app/shared/services/media-mode.service';
import { mediaImageUrl } from 'src/app/shared/utils/media-url';
import { MediaItem } from '../../models/media-item.model';
import { MediaLibraryViewService } from '../../services/media-library-view.service';

@Component({
  selector: 'app-library-card',
  templateUrl: './library-card.component.html',
  styleUrls: ['./library-card.component.scss'],
  imports: [IonBadge, IonButton, IonIcon],
})
export class LibraryCardComponent {
  readonly view = inject(MediaLibraryViewService);

  @Input({ required: true }) item!: MediaItem;
  @Input({ required: true }) mediaMode!: MediaModeOption;
  @Input() isAdding = false;

  @Output() edit = new EventEmitter<MediaItem>();
  @Output() details = new EventEmitter<MediaItem>();

  imageFor(item: MediaItem): string {
    return mediaImageUrl(item.image);
  }
}
