import { IStore } from "utils/basicStore";
import { MyGame } from "utils/games";

export const useMyGameStore = defineStore("myGames", (): IStore<MyGame> => {
  const MediaList: Ref<MyGame[] | null> = ref(null)
  const PageIndex: Ref<number> = ref(1)
  const TotalPages: Ref<number> = ref(1)
  const SelectedGame: Ref<MyGame | null> = ref(null)
  const Prefix: string = "MyGames"
  const TableColumns = [
    { key: "rating", label: "Rating" },
    { key: "startDate", label: "Start" },
    { key: "timeSpend", label: "Playtime" },
    { key: "users", label: "Users" },
    { key: "status", label: "Progress" }
]

  const Media = computed(() => {
    return MediaList.value
  })

  async function getMedia(howMany?: number) : Promise<MyGame[]> {
    let response = await fetchMedia<MyGame>(howMany ?? 100, Prefix)
    if(typeof response === 'object' && response != null)
      MediaList.value = response
    return response;
  }

  async function getPaginatedMedia() : Promise<MyGame[]> {
    let response = await fetchPaginatedMedia<MyGame>(Prefix)
    if(typeof response === 'object' && response != null)
      MediaList.value = response.data
      PageIndex.value = response.currentPage
      TotalPages.value = response.pages
    return response.data;
  }

  async function getMediaById(id: number): Promise<MyGame | undefined> {
    return await fetchMediaById<MyGame>(id, Prefix)
  }
  
  async function removeMedia(id: number) {
    await deleteMedia(id, Prefix)
    await getMedia()
    return;
  }

  async function createMedia(media: MyGame) {
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
