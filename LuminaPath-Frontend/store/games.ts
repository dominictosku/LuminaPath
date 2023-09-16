import { IStore } from "~/utils/interfaces/IBasicStore";
import { IGame } from "~/utils/games";

export const useGameStore = defineStore("games", (): IStore<IGame> => {
  const MediaList: Ref<IGame[] | null> = ref(null)
  const PageIndex: Ref<number> = ref(1)
  const TotalPages: Ref<number> = ref(1)
  const SelectedGame: Ref<IGame | null> = ref(null)
  const Prefix: string = "games"
  const TableColumns = [
    { key: "status", label: "Status" },
    { key: "platform", label: "Plattform" },
    { key: "playtime", label: "Playtime" },
    { key: "users", label: "Users" },
    { key: "progress", label: "Progress" }
]

  const Media = computed(() => {
    return MediaList.value
  })

  async function getMedia(howMany?: number) : Promise<IGame[]> {
    let response = await fetchMedia<IGame>(howMany ?? 100, Prefix)
    if(typeof response === 'object' && response != null)
      MediaList.value = response
    return response;
  }

  async function getPaginatedMedia() : Promise<IGame[]> {
    let response = await fetchPaginatedMedia<IGame>(Prefix)
    if(typeof response === 'object' && response != null)
      MediaList.value = response.data
      PageIndex.value = response.currentPage
      TotalPages.value = response.pages
    return response.data;
  }

  async function getMediaById(id: number): Promise<IGame | undefined> {
    return await fetchMediaById<IGame>(id, Prefix)
  }
  
  async function removeMedia(id: number) {
    await deleteMedia(id, Prefix)
    await getMedia()
    return;
  }

  async function createMedia(media: IGame) {
    if(media.id == 0){
      await PostMedia(media, Prefix)
    }else{
      await PutMedia(media, Prefix)
    }
    await getMedia()
    return;
  }

  return { MediaList: Media, SelectedGame, PageIndex, TotalPages, TableColumns,
    getMedia, getPaginatedMedia, getMediaById, createMedia, removeMedia 
    }
});
