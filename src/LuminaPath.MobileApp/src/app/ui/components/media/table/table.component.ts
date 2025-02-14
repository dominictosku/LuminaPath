import { Component, OnInit } from '@angular/core';
import { GameService } from 'src/app/core/services/game.service';
import { Game } from 'src/app/core/models/games';

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
