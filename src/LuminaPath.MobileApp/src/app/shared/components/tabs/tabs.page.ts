import { Component, EnvironmentInjector, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IonTabs, IonTabBar, IonTabButton, IonIcon, IonLabel } from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  albums,
  albumsOutline,
  calendarClear,
  calendarClearOutline,
  gameController,
  gameControllerOutline,
  library,
  libraryOutline,
  sparkles,
  sparklesOutline,
} from 'ionicons/icons';

@Component({
    selector: 'app-tabs',
    templateUrl: 'tabs.page.html',
    styleUrls: ['tabs.page.scss'],
    imports: [RouterLink, IonTabs, IonTabBar, IonTabButton, IonIcon, IonLabel]
})
export class TabsPage {
  public environmentInjector = inject(EnvironmentInjector);

  constructor() {
    addIcons({
      albums,
      albumsOutline,
      calendarClear,
      calendarClearOutline,
      gameController,
      gameControllerOutline,
      library,
      libraryOutline,
      sparkles,
      sparklesOutline,
    });
  }
}
