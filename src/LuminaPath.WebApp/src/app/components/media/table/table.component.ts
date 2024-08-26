import { Component, OnInit } from '@angular/core';
import { GameService } from 'src/app/core/services/game.service';
import { GamesComponent } from "../data/games/games.component";

@Component({
  standalone: true,
  selector: 'app-table',
  templateUrl: './table.component.html',
  styleUrls: ['./table.component.scss'],
  imports: [GamesComponent],
})
export class TableComponent implements OnInit {

  constructor(private gameService: GameService) { }

  ngOnInit() { }

  medias = this.gameService.get()
  labels = this.gameService.labels

  route() {
  }
}
