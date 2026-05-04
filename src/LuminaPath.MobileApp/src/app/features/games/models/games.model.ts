import { type IBasicInfo } from "src/app/core/interfaces/iBasicInfo";
import { type IGame } from "../interfaces/iGames";
import { MediaFile } from "../../media/models/mediaFile.model";

export class Game implements IGame, IBasicInfo {
  id: number;
  name: string;
  description: string;
  releaseDate: Date;
  genre: string;
  platforms: number;
  playtime: number;
  myGames: MyGame | null;
  image: MediaFile | null;

  constructor(
    name?: string | null,
    description?: string | null,
    releaseDate?: Date | null,
    genre?: string | null,
    platforms?: number | null,
    playtime?: number | null,
    myGame?: MyGame | null) {
    this.id = 0;
    this.name = name ?? "";
    this.description = description ?? "";
    this.releaseDate = releaseDate ?? new Date;
    this.genre = genre ?? "";
    this.platforms = platforms ?? 0;
    this.playtime = playtime ?? 0;
    this.myGames = myGame ?? null;
    this.image = null;
  }
}

export class MyGame implements IBasicInfo {
  id: number;
  rating: number | null;
  startDate: Date | null;
  endDate: Date | null;
  status: number;
  timeSpend: number | null;
  gameId: number;
  game: Game | null;
  myGameInfo: MyGameInfo | null;

  constructor(gameId: number) {
    this.id = 0;
    this.rating = null;
    this.startDate = null;
    this.endDate = null;
    this.status = 1;
    this.timeSpend = 0;
    this.game = null;
    this.gameId = gameId;
    this.myGameInfo = null;
  }
}

export class MyGameInfo {
  id: number;
  myGameId: number;
  trackedHours: number;
  firstPlayed: Date | null;
  lastPlayed: Date | null;

  constructor() {
    this.id = 0;
    this.myGameId = 0;
    this.trackedHours = 0;
    this.firstPlayed = null;
    this.lastPlayed = null;
  }
}

export const Platforms = [
  { label: "Playstation", value: 0 },
  { label: "Switch", value: 1 },
  { label: "PC", value: 2 },
  { label: "XBOX", value: 3 },
];
