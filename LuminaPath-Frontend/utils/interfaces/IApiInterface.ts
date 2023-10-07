import { IBasicInfo } from "./iBasicInfo";
import { PaginateResult } from "~/utils/classes/paginatedResult";

export interface IApi<T> {
    getMedia(endPoint: string, mediaFilter?: MediaFilter): Promise<PaginateResult<IBasicInfo>>;
    getMediaAll(endPoint: string, count?: number): Promise<T[]>;
    getMediaById(id: number, endPoint: string): Promise<T | undefined>;
    removeMedia(id: number, endPoint: string): Promise<void>;
    createMedia(media: IBasicInfo, endPoint: string): Promise<void>;
  }