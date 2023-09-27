import { MyGame } from "~/utils/model/games";
import { BaseStore } from "~/utils/classes/baseStore";
import { IStore } from "~/utils/interfaces/IBasicStore";

export const useMyGameStore = defineStore("myGames", (): IStore<MyGame> => {
  const Id: String = "MyGames";
  const { Media, PageIndex, TotalPages, api } = BaseStore<MyGame>("MyGames");
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
    getMedia: api.getMedia,
    getMediaAll: api.getMediaAll,
    getPaginatedMedia: api.getPaginatedMedia,
    getMediaById: api.getMediaById,
    createMedia: api.createMedia,
    removeMedia: api.removeMedia,
  };
});
