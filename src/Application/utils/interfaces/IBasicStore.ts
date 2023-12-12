import { MediaComponent } from "~/utils/classes/mediaComponent"
import { IApi } from "./IApiInterface"
import { IBasicInfo } from "./iBasicInfo"
import {MediaFilter} from "~/utils/classes/mediaFilter"

export interface IStore<T>{
    Id:  globalThis.ComputedRef<string>
    Media: globalThis.Ref<T[]>
    Filter: globalThis.Ref<MediaFilter>
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