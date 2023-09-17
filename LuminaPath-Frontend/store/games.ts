import { BaseStore } from "~/utils/baseStore";
import { IStore, IMainStore } from "~/utils/interfaces/IBasicStore";
import { IGame } from "~/utils/interfaces/iGames";
import { useMyGameStore } from "./myGames";
import { MyGame } from "#imports";

export const useGameStore = defineStore("games", (): IMainStore<MyGame> & IStore<IGame> => {
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
  const MyStore: any = useMyGameStore()
  const SelectedGame: Ref<IGame | null> = ref(null);
  const TableColumns = [
    { key: "status", label: "Status" },
    { key: "platform", label: "Plattform" },
    { key: "playtime", label: "Playtime" },
    { key: "users", label: "Users" },
    { key: "progress", label: "Progress" },
  ];
  const Formtype: String = "GameForms"

  return {
    MediaList: Media,
    SelectedGame,
    PageIndex,
    TotalPages,
    TableColumns,
    MyStore,
    Formtype,
    getMedia,
    getPaginatedMedia,
    getMediaById,
    createMedia,
    removeMedia,
  };
});
