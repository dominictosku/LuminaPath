import { IStore } from "~/utils/interfaces/IBasicStore";
import { IGame } from "~/utils/interfaces/iGames";
type TypeMap = {
  Game: IStore<IGame>;
};

export function getStoreType<T extends keyof TypeMap>(
  store?: Object
): TypeMap[T] {
  return store as TypeMap[T];
}
