import { MyGame } from "~/utils/games";
import { BaseStore } from "~/utils/baseStore";
import { IStore } from "~/utils/interfaces/IBasicStore";

export const useMyGameStore = defineStore("myGames", (): IStore<MyGame> => {
  const {
    Media,
    PageIndex,
    TotalPages,
    getMedia,
    getPaginatedMedia,
    getMediaById,
    createMedia,
    removeMedia,
  } = BaseStore<MyGame>("MyGames");
  const SelectedGame: Ref<MyGame | null> = ref(null);
  const TableColumns = [
    { key: "status", label: "Status" },
    { key: "platform", label: "Plattform" },
    { key: "playtime", label: "Playtime" },
    { key: "users", label: "Users" },
    { key: "progress", label: "Progress" },
  ];
  const Formtype: String = "MyGameForms"

  return {
    MediaList: Media,
    SelectedGame,
    PageIndex,
    TotalPages,
    TableColumns,
    Formtype,
    getMedia,
    getPaginatedMedia,
    getMediaById,
    createMedia,
    removeMedia,
  };
});
