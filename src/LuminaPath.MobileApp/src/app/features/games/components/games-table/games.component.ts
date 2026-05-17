import { Component, Input } from '@angular/core';
import { Game } from '../../models/games.model';

@Component({
  selector: 'app-games',
  templateUrl: './games.component.html',
  styleUrls: ['./games.component.scss'],
  standalone: true,
})
export class GamesComponent {
  @Input() media = new Game();

  route() {
  }
}
