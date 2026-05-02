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
    this.myGames = myGame ?? new MyGame(0);
    this.image = null;
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

export const Platforms = [
  { label: "Playstation", value: 0 },
  { label: "Switch", value: 1 },
  { label: "PC", value: 2 },
  { label: "XBOX", value: 3 },
];
