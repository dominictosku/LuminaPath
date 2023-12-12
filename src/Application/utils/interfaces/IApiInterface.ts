import { type IBasicInfo } from "./iBasicInfo";
import { PaginateResult } from "~/utils/classes/paginatedResult";
import { MediaFilter } from "~/utils/classes/mediaFilter";

export interface IApi<T> {
    getMedia(endPoint: string, mediaFilter?: MediaFilter): Promise<PaginateResult<IBasicInfo>>;
    getMediaById(id: number, endPoint: string): Promise<T | undefined>;
    removeMedia(id: number, endPoint: string): Promise<void>;
    createMedia(media: IBasicInfo, endPoint: string): Promise<void>;
  }