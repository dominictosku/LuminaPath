import { Component } from '@angular/core';
import { Game } from 'src/app/features/games/models/games.model';

@Component({
  selector: 'app-my-games',
  templateUrl: './my-games.component.html',
  styleUrls: ['./my-games.component.scss'],
  standalone: true,
})
export class MyGamesComponent {
  modalProps = { game: "mygames", form: "myMedia", id: 1 }
  media = new Game()
  openModal(object: any, id: any) {

  }
}
