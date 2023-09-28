import { IBasicInfo } from "./iBasicInfo";

export interface IApi<T> {
    getMedia(mediaFilter?: MediaFilter): Promise<IBasicInfo[]>;
    getMediaAll(count?: number): Promise<T[]>;
    getPaginatedMedia(mediaFilter: MediaFilter): Promise<T[]>;
    getMediaById(id: number): Promise<T | undefined>;
    removeMedia(id: number): Promise<void>;
    createMedia(media: IBasicInfo): Promise<void>;
  }