import { MediaComponent } from "~/utils/classes/mediaComponent"
import { IApi } from "./IApiInterface"
import { IBasicInfo } from "./iBasicInfo"

export interface IStore<T>{
    Id:  globalThis.ComputedRef<string>
    Media: globalThis.Ref<T[]>
    PageIndex: Ref<number>
    TotalPages: Ref<number>
    Type: {
        Games: string,
        MyGames: string,
      };
    ActiveComponent: Ref<MediaComponent> 
    GameComponents: MediaComponent
    MyGameComponents: MediaComponent
    Api: IApi<IBasicInfo>
    changeMode(): string
}