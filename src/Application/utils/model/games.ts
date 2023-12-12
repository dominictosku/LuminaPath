import { type IBasicInfo } from "~/utils/interfaces/iBasicInfo";
import { type IGame } from "~/utils/interfaces/iGames";

export class Game implements IGame, IBasicInfo {
  id: number;
  name: string;
  description: string;
  releaseDate: Date;
  genre: string;
  plattforms: number;
  playtime: number;
  myGames: MyGame | null;

  constructor(myGame?: MyGame) {
    this.id = 0;
    this.name = "";
    this.description = "";
    this.releaseDate = new Date;
    this.genre = "";
    this.plattforms = 0;
    this.playtime = 0;
    this.myGames = myGame ?? new MyGame(0);
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

  constructor(gameId: number) {
    this.id = 0;
    this.rating = 0;
    this.startDate = new Date;
    this.endDate = new Date;
    this.status = 0;
    this.timeSpend = 0;
    this.game = null;
    this.gameId = gameId;
  }
}

export const Plattforms = [
  { label: "Playstation", value: 0 },
  { label: "Switch", value: 1 },
  { label: "PC", value: 2 },
  { label: "XBOX", value: 3 },
];
