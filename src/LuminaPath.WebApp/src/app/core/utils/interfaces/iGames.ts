import { MyGame } from "../model/games";
export interface IGame {
  id: number;
  name: string;
  description: string;
  genre: string;
  plattforms: number;
  playtime: number;
  myGames: MyGame | null
}
