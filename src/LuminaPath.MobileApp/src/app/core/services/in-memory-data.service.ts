import { Injectable } from '@angular/core';
import { Game } from '../models/games';
import { PaginateResult } from '../entities/paginatedResult';

@Injectable({
  providedIn: 'root'
})
export class InMemoryDataService {

  constructor() { }

  createDb() {
    let data = [
      new Game("Test", "Test"),
      new Game("Test", "Test"),
      new Game("Test", "Test"),
      new Game("Test", "Test"),
      new Game("Test", "Test"),
      new Game("Test", "Test")
    ]
    let games = new PaginateResult<Game>();
    games.data = data;
    games.PageIndex = 1;
    games.TotalPages = 1;
    return { games };
  }

  // Overrides the genId method to ensure that a hero always has an id.
  // If the heroes array is empty,
  // the method below returns the initial number (11).
  // if the heroes array is not empty, the method below returns the highest
  // hero id + 1.
  genId(games: Game[]): number {
    return games.length > 0 ? Math.max(...games.map(game => game.id)) + 1 : 11;
  }
}
