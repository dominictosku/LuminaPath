import { MyGame } from "../models/games.model";
export interface IGame {
  id: number;
  name: string;
  description: string;
  genre: string;
  platforms: number;
  playtime: number;
  myGames: MyGame | null
}
