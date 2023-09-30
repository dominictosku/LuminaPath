import { IBasicInfo } from "./iBasicInfo";

export interface IApi<T> {
    getMedia(endPoint: string, mediaFilter?: MediaFilter): Promise<IBasicInfo[]>;
    getMediaAll(endPoint: string, count?: number): Promise<T[]>;
    getPaginatedMedia(mediaFilter: MediaFilter, endPoint: string,): Promise<T[]>;
    getMediaById(id: number, endPoint: string): Promise<T | undefined>;
    removeMedia(id: number, endPoint: string): Promise<void>;
    createMedia(media: IBasicInfo, endPoint: string): Promise<void>;
  }