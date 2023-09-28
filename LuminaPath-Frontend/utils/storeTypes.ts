import { MyGame } from '~/utils/model/games';
import { IStore } from '~/utils/interfaces/IBasicStore';
import { IGame } from '~/utils/interfaces/iGames';
type TypeMap = {
    Game: IStore<IGame, MyGame>,
};

export function getStoreType<T extends keyof TypeMap>(typeName: T, store?: Object): TypeMap[T] {
    return store as TypeMap[T];
  }