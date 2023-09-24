import { IBasicInfo } from "~/utils/interfaces/iBasicInfo";
import { IGame } from "~/utils/interfaces/iGames";

export class Game implements IGame, IBasicInfo {
  id: number;
  name: string;
  description: string;
  genre: string;
  plattforms: number;
  playtime: number;
  myGames: MyGame | null

  constructor(
    id: number,
    name: string,
    description: string,
    genre: string,
    plattforms: number,
    playtime: number,
    myGame?: MyGame
  ) {
    this.id = id;
    this.name = name;
    this.description = description;
    this.genre = genre;
    this.plattforms = plattforms;
    this.playtime = playtime;
    this.myGames = myGame ?? null
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
  game: Game | null;

  constructor(
    id: number,
    rating: number,
    startDate: Date,
    endDate: Date,
    status: number,
    timeSpend: number,
    gameId: number,
    game?: Game
  ) {
    this.id = id;
    this.rating = rating;
    this.startDate = startDate;
    this.endDate = endDate;
    this.status = status;
    this.timeSpend = timeSpend;
    this.gameId = gameId;
    this.game = game ?? null;
  }
}

export const Plattforms = [
    { label: 'Playstation', value: 0 },
    { label: 'Switch', value: 1 },
    { label: 'PC', value: 2 },
    { label: 'XBOX', value: 3 }
]
