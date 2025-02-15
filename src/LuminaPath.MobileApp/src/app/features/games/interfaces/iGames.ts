import { MyGame } from "../models/games.model";
export interface IGame {
  id: number;
  name: string;
  description: string;
  genre: string;
  plattforms: number;
  playtime: number;
  myGames: MyGame | null
}
