export interface IStore<T>{
    MediaList: Ref<T[] | null>
    PageIndex: Ref<number>
    TotalPages: Ref<number>
    SelectedGame: Ref<T | null>
    getMedia(howMany?: number) : Promise<T[]>
    getMediaById(id: number): Promise<T | undefined>
    getPaginatedMedia() : Promise<T[]>
    createMedia(media: T) : any
    removeMedia(id: number): any
}