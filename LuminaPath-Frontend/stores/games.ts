import { IGame } from "utils/games";

export const useGameStore = defineStore("games", () => {
  const GamesList: Ref<IGame[] | null> = ref(null)
  const SelectedGame: Ref<IGame | null> = ref(null)
  async function getGames() : Promise<IGame[]> {
    let data = await fetchGames()
    if(typeof data === 'object' && data != null)
      GamesList.value = data
    return data;
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
    await PostGame(game)
    await getGames()
    return;
  }

  return { GamesList, SelectedGame, getGames, createGame, getGameById, removeGame }
});
