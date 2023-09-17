export interface IStore<T>{
    Id: String
    MediaList: Ref<T[] | null>
    PageIndex: Ref<number>
    TotalPages: Ref<number>
    SelectedGame: Ref<T | null>
    TableColumns: Array<Object>
    getMedia(howMany?: number) : Promise<T[]>
    getMediaById(id: number): Promise<T | undefined>
    getPaginatedMedia() : Promise<T[]>
    createMedia(media: T) : any
    removeMedia(id: number): any
}

export interface IMainStore<T>{
    MyStore: IStore<T>
}