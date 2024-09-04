import { Component, Input, OnInit } from '@angular/core';
import { Game } from 'src/app/core/models/games';

@Component({
  selector: 'app-games',
  templateUrl: './games.component.html',
  styleUrls: ['./games.component.scss'],
  standalone: true,
})
export class GamesComponent implements OnInit {

  constructor() { }

  ngOnInit() { }
  @Input('media') media = new Game();
  route() {
  }
}
