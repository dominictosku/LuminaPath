import { type IGame } from "@/utils/models/games";
import { PostGame, fetchGames } from "@/services/request";

export const useGameStore = defineStore("games", () => {
  const GamesList: Ref<IGame[] | null> = ref(null)
  async function getGames() : Promise<IGame[]> {
    let data = await fetchGames()
    if(typeof data === 'object' && data != null)
      GamesList.value = data
    return data;
  }

  async function createGame(game: IGame) {
    await PostGame(game)
    await getGames()
    return;
  }

  return { GamesList, getGames, createGame }
});
