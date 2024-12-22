import { MediaFilter } from "../entities/mediaFilter";
import { PaginateResult } from "../entities/paginatedResult";
import { type IBasicInfo } from "./iBasicInfo";

export interface IApi<T> {
  getMedia(mediaFilter?: MediaFilter): Promise<PaginateResult<IBasicInfo>>;
  getMediaById(id: number, endPoint: string): Promise<T | undefined>;
  removeMedia(id: number, endPoint: string): Promise<void>;
  createMedia(media: IBasicInfo, endPoint: string): Promise<void>;
}
