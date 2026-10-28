import { Component, input, output } from '@angular/core';
import { EpisodicMediaDetailsPage } from '../../pages/episodic-media-details/episodic-media-details.page';
import { animeDetailsProviders } from '../../../animes/pages/anime-details.providers';
import { seriesDetailsProviders } from '../../../series/pages/series-details.providers';

@Component({
  selector: 'app-anime-details-modal',
  standalone: true,
  imports: [EpisodicMediaDetailsPage],
  providers: animeDetailsProviders,
  styles: [`
    :host {
      display: flex;
      flex-direction: column;
      width: 100%;
      height: 100%;
      flex: 1 1 100%;
      min-height: 0;
      position: relative;
    }
    app-episodic-media-details {
      display: flex;
      flex-direction: column;
      width: 100%;
      height: 100%;
      flex: 1 1 100%;
      min-height: 0;
      position: relative;
    }
  `],
  template: `
    <app-episodic-media-details
      class="ion-page"
      [mediaId]="mediaId()"
      [isModal]="true"
      (dismiss)="dismiss.emit()" />
  `,
})
export class AnimeDetailsModalComponent {
  readonly mediaId = input<number | string>();
  readonly dismiss = output<void>();
}

@Component({
  selector: 'app-series-details-modal',
  standalone: true,
  imports: [EpisodicMediaDetailsPage],
  providers: seriesDetailsProviders,
  styles: [`
    :host {
      display: flex;
      flex-direction: column;
      width: 100%;
      height: 100%;
      flex: 1 1 100%;
      min-height: 0;
      position: relative;
    }
    app-episodic-media-details {
      display: flex;
      flex-direction: column;
      width: 100%;
      height: 100%;
      flex: 1 1 100%;
      min-height: 0;
      position: relative;
    }
  `],
  template: `
    <app-episodic-media-details
      class="ion-page"
      [mediaId]="mediaId()"
      [isModal]="true"
      (dismiss)="dismiss.emit()" />
  `,
})
export class SeriesDetailsModalComponent {
  readonly mediaId = input<number | string>();
  readonly dismiss = output<void>();
}
