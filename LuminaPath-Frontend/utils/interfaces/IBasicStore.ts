import { IApi } from "./IApiInterface"
import { IBasicInfo } from "./iBasicInfo"

export interface IStore<T, T2>{
    Id:  globalThis.ComputedRef<string>
    Media: globalThis.Ref<T[] | null>
    MyMedia: globalThis.Ref<T2[] | null>
    PageIndex: Ref<number>
    TotalPages: Ref<number>
    SelectedGame: Ref<T | null>
    TableColumns: Array<Object>
    Api: IApi<IBasicInfo>
    changeMode(): string
}