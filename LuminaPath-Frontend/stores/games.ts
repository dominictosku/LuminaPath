import { type Game } from "@/utils/models/games";
import { fetchGames } from "@/services/request";

export const useGameStore = defineStore("games", () => {
  const GamesList: Ref<Game[] | null> = ref(null)
  async function getGames() : Promise<Game[]> {
    let data = await fetchGames()
    GamesList.value = data
    return data;
  }

  return { GamesList, getGames }
});
