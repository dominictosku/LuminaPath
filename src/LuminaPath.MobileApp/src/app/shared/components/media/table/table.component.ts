import { Component, OnInit } from '@angular/core';
import { GameService } from 'src/app/features/games/services/game.service';
import { Game } from 'src/app/features/games/models/games.model';
import { ImportsModule } from 'src/app/import';

@Component({
  selector: 'app-table',
  templateUrl: './table.component.html',
  styleUrls: ['./table.component.scss'],
  imports: [ImportsModule]
})
export class TableComponent implements OnInit {
  medias: Game[] = [];
  labels = this.gameService.labels;

  constructor(private gameService: GameService) {
    this.getGames();
  }

  ngOnInit() {}

  getGames() {
    this.gameService.getAll().subscribe((event: any) => {
      this.medias = event.data;
    });
  }
  route() {}
}
