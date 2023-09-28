import { BaseStore } from "~/utils/classes/baseStore";
import { IStore } from "~/utils/interfaces/IBasicStore";
import { IGame } from "~/utils/interfaces/iGames";
import { MyGame } from "#imports";

export const useGameStore = defineStore("games", (): IStore<IGame, MyGame> => {
  const Type = {
    Games: "Games",
    MyGames: "MyGames",
  };
  const { Id: id, Media, MyMedia, Mode, PageIndex, TotalPages, Api } = BaseStore<
    IGame,
    MyGame
  >("games");
  const Id = computed(() => {
    return id.value
  })
  const SelectedGame: Ref<IGame | null> = ref(null);
  const MediaColumns = [
    { key: "status", label: "Status" },
    { key: "platform", label: "Plattform" },
    { key: "playtime", label: "Playtime" },
    { key: "users", label: "Users" },
    { key: "progress", label: "Progress" },
  ];
  
  const MyMediaColumns = [
    { key: "status", label: "Status" },
    { key: "platform", label: "Plattform" },
    { key: "playtime", label: "Playtime" },
    { key: "users", label: "Users" },
    { key: "progress", label: "Progress" },
  ];
  let TableColumns = MediaColumns
  
  function changeMode() : string {
    if (Mode.value == "Media") {
      Mode.value = "MyMedia";
      id.value = Type.MyGames;
      TableColumns = MyMediaColumns
      return "MyMedia"
    } else {
      Mode.value = "Media";
      id.value = Type.Games;
      TableColumns = MediaColumns
      return "Media"
    }
  }

  return {
    Id,
    Media,
    MyMedia,
    SelectedGame,
    PageIndex,
    TotalPages,
    TableColumns,
    Api,
    changeMode
  };
});
