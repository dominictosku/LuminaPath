import { ChangeDetectionStrategy, Component } from '@angular/core';
import { IonIcon } from '@ionic/angular/standalone';

/** Collection introduction with projected page actions. */
@Component({
  selector: 'app-library-hero',
  templateUrl: './library-hero.component.html',
  styleUrls: ['./library-hero.component.scss'],
  imports: [IonIcon],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LibraryHeroComponent {}
