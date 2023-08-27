import { IBasicInfo } from "~/utils/basicInfo";

export interface IGame {
  id: number;
  name: string;
  description: string;
  genre: string;
  plattforms: number;
  playtime: number;
}

export class Game implements IGame, IBasicInfo {
  id: number;
  name: string;
  description: string;
  genre: string;
  plattforms: number;
  playtime: number;

  constructor(
    id: number,
    name: string,
    description: string,
    genre: string,
    plattforms: number,
    playtime: number
  ) {
    this.id = id;
    this.name = name;
    this.description = description;
    this.genre = genre;
    this.plattforms = plattforms;
    this.playtime = playtime;
  }
}

export class MyGame implements IBasicInfo {
  id: number;
  rating: number;
  startDate: Date;
  endDate: Date;
  status: number;
  timeSpend: number;
  gameId: number;
  game: Game;

  constructor(
    id: number,
    rating: number,
    startDate: Date,
    endDate: Date,
    status: number,
    timeSpend: number,
    gameId: number
  ) {
    this.id = id;
    this.rating = rating;
    this.startDate = startDate;
    this.endDate = endDate;
    this.status = status;
    this.timeSpend = timeSpend;
    this.gameId = gameId;
    this.game = new Game(0, "", "", "", 0, 0)
  }
}

export const Plattforms = [
    { label: 'Playstation', value: 0 },
    { label: 'Switch', value: 1 },
    { label: 'PC', value: 2 },
    { label: 'XBOX', value: 3 }
]
