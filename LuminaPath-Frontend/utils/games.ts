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

export const Plattforms = [
    { label: 'Playstation', value: 0 },
    { label: 'Switch', value: 1 },
    { label: 'PC', value: 2 },
    { label: 'XBOX', value: 3 }
]
