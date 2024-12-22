import { MediaComponent } from "~/utils/classes/mediaComponent"
import { type IApi } from "./IApi"
import { type IBasicInfo } from "./iBasicInfo"
import { MediaFilter } from "~/utils/classes/mediaFilter"
import { Pagination } from "../entities/pagination"

export interface IStore<T>{
    Id:  globalThis.ComputedRef<string>
    Media: globalThis.Ref<T[]>
    Filter: globalThis.Ref<MediaFilter>
    Paging: globalThis.Ref<Pagination>
    Type: {
        Games: string,
        MyGames: string,
      };
    ActiveComponent: Ref<MediaComponent>
    Api: IApi<IBasicInfo>
    changeMode(): string
}
