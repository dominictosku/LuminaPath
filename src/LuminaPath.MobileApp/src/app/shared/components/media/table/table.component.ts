import { Component, OnInit } from '@angular/core';
import { GameService } from 'src/app/features/games/services/game.service';
import { Game } from 'src/app/features/games/models/games.model';

@Component({
  selector: 'app-table',
  templateUrl: './table.component.html',
  styleUrls: ['./table.component.scss'],
})
export class TableComponent implements OnInit {
  constructor(private gameService: GameService) {
    this.getGames();
  }

  ngOnInit() {}

  getGames() {
    this.gameService.getMedia().subscribe((event: any) => {
      this.medias = event.data;
    });
  }

  medias: Game[] = [];
  labels = this.gameService.labels;

  route() {}
}
