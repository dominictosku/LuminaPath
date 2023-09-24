import { MyGame } from "~/utils/games";
import { BaseStore } from "~/utils/baseStore";
import { IStore } from "~/utils/interfaces/IBasicStore";

export const useMyGameStore = defineStore("myGames", (): IStore<MyGame> => {
  const Id: String = "MyGames"
  const {
    Media,
    PageIndex,
    TotalPages,
    getMedia,
    getMediaAll,
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

  return {
    Id,
    MediaList: Media,
    SelectedGame,
    PageIndex,
    TotalPages,
    TableColumns,
    getMedia,
    getMediaAll,
    getPaginatedMedia,
    getMediaById,
    createMedia,
    removeMedia,
  };
});
