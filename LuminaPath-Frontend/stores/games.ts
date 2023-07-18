import { type Game } from "@/utils/models/games";

export const useGameStore = defineStore("game", {
  state: () => {
    return {
      // for initially empty lists
      GamesList: [] as Game[],
      // for data that is not yet loaded
      Game: null as Game | null,
      count: 0,
    };
  },
});
