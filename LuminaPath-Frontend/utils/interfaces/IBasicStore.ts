import { MediaComponent } from "~/utils/classes/mediaComponent"
import { IApi } from "./IApiInterface"
import { IBasicInfo } from "./iBasicInfo"

export interface IStore<T, T2>{
    Id:  globalThis.ComputedRef<string>
    Media: globalThis.Ref<T[] | null>
    MyMedia: globalThis.Ref<T2[] | null>
    PageIndex: Ref<number>
    TotalPages: Ref<number>
    SelectedGameId: Ref<number>
    ActiveComponent: Ref<MediaComponent> 
    Api: IApi<IBasicInfo>
    changeMode(): string
}