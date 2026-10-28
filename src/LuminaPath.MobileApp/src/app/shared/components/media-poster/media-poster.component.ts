import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';

/** Shared artwork treatment for library and release feeds; no persistence. */
@Component({
  selector: 'app-media-poster',
  imports: [IonIcon],
  templateUrl: './media-poster.component.html',
  styleUrl: './media-poster.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MediaPosterComponent {
  readonly title = input.required<string>();
  readonly image = input.required<string>();
  readonly kind = input.required<string>();
  readonly icon = input('albums-outline');
  readonly subtitle = input('');
  readonly meta = input('');
  readonly status = input('');
  readonly progress = input<number | null>(null);
  readonly selected = input(false);
  readonly selecting = input(false);
  readonly disabled = input(false);
  readonly actionLabel = input('Open details');
  readonly actionIcon = input('ellipsis-horizontal');
  readonly open = output<void>();
  readonly action = output<void>();

  imageFailed(event: Event): void {
    const image = event.target as HTMLImageElement;
    if (image.getAttribute('src') !== 'assets/png/Placeholder.png') image.src = 'assets/png/Placeholder.png';
  }
}
