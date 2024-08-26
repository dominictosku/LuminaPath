import { Injectable } from '@angular/core';
import { Game } from '../utils/model/games';

@Injectable({
  providedIn: 'root'
})
export class GameService {

  constructor() { }
  public labels = [
    "Title",
    "Description",
    "Status",
    "Release"
  ]

  public get(): Game[] {
    var games = [
      new Game("Test", "Test"),
      new Game("Test", "Test"),
      new Game("Test", "Test"),
      new Game("Test", "Test"),
      new Game("Test", "Test"),
      new Game("Test", "Test")
    ]
    return games;
  }
}
