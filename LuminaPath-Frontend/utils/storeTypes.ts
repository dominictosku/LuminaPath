import { MyGame } from '~/utils/model/games';
import { IMainStore, IStore } from '~/utils/interfaces/IBasicStore';
import { IGame } from '~/utils/interfaces/iGames';
type TypeMap = {
    game: IStore<IGame> & IMainStore<MyGame>;
    myGame: IStore<MyGame>;
};

export function getStoreType<T extends keyof TypeMap>(typeName: T, store?: Object): TypeMap[T] {
    return store as TypeMap[T];
  }