import { IGame } from "utils/games";

export const useGameStore = defineStore("games", () => {
  const GamesList: Ref<IGame[] | null> = ref(null)
  const PageIndex: Ref<number> = ref(1)
  const TotalPages: Ref<number> = ref(1)
  const SelectedGame: Ref<IGame | null> = ref(null)
  const Prefix: string = "games"

  async function getGames(howMany?: number) : Promise<IGame[]> {
    let response = await fetchMedia<IGame>(howMany ?? 100, Prefix)
    if(typeof response === 'object' && response != null)
      GamesList.value = response
    return response;
  }

  async function getPaginatedGames() : Promise<IGame[]> {
    let response = await fetchPaginatedMedia<IGame>(Prefix)
    if(typeof response === 'object' && response != null)
      GamesList.value = response.data
      PageIndex.value = response.currentPage
      TotalPages.value = response.pages
    return response.data;
  }

  async function getGameById(id: number): Promise<IGame | undefined> {
    return await fetchMediaById<IGame>(id, Prefix)
  }
  
  async function removeGame(id: number) {
    await deleteMedia(id, Prefix)
    await getGames()
    return;
  }

  async function createGame(game: IGame) {
    if(game.id == 0){
      await PostMedia(game, Prefix)
    }else{
      await PutMedia(game, Prefix)
    }
    await getGames()
    return;
  }

  return { GamesList, SelectedGame, PageIndex, TotalPages,
     getGames, getPaginatedGames, createGame, getGameById, removeGame 
    }
});
