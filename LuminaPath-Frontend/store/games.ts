import { IGame } from "utils/games";

export const useGameStore = defineStore("games", () => {
  const GamesList: Ref<IGame[] | null> = ref(null)
  const PageIndex: Ref<number> = ref(1)
  const TotalPages: Ref<number> = ref(1)
  const SelectedGame: Ref<IGame | null> = ref(null)

  async function getGames(howMany?: number) : Promise<IGame[]> {
    let response = await fetchGames(howMany ?? 100)
    if(typeof response === 'object' && response != null)
      GamesList.value = response
    return response;
  }

  async function getPaginatedGames() : Promise<IGame[]> {
    let response = await fetchPaginatedGames()
    if(typeof response === 'object' && response != null)
      GamesList.value = response.data
      PageIndex.value = response.currentPage
      TotalPages.value = response.pages
    return response.data;
  }

  function getGameById(id: number): IGame | undefined {
    if(GamesList.value != null){
      return GamesList.value.find(g => g.id == id)
    }
    return undefined
  }
  
  async function removeGame(id: number) {
    await deleteGame(id)
    await getGames()
    return;
  }

  async function createGame(game: IGame) {
    if(game.id == 0){
      await PostGame(game)
    }else{
      await PutGame(game)
    }
    await getGames()
    return;
  }

  return { GamesList, SelectedGame, PageIndex, TotalPages,
     getGames, getPaginatedGames, createGame, getGameById, removeGame 
    }
});
