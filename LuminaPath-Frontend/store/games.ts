import { BaseStore } from "~/utils/baseStore";
import { IStore } from "~/utils/interfaces/IBasicStore";
import { IGame } from "~/utils/interfaces/iGames";

export const useGameStore = defineStore("games", (): IStore<IGame> => {
  const {
    Media,
    PageIndex,
    TotalPages,
    getMedia,
    getPaginatedMedia,
    getMediaById,
    createMedia,
    removeMedia,
  } = BaseStore<IGame>("games");
  const SelectedGame: Ref<IGame | null> = ref(null);
  const TableColumns = [
    { key: "status", label: "Status" },
    { key: "platform", label: "Plattform" },
    { key: "playtime", label: "Playtime" },
    { key: "users", label: "Users" },
    { key: "progress", label: "Progress" },
  ];

  return {
    MediaList: Media,
    SelectedGame,
    PageIndex,
    TotalPages,
    TableColumns,
    getMedia,
    getPaginatedMedia,
    getMediaById,
    createMedia,
    removeMedia,
  };
});
