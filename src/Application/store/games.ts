import { BaseStore } from "~/utils/classes/baseStore";
import { type IStore } from "~/utils/interfaces/IStore";
import { Game } from "#imports";
import { MediaComponent } from "~/utils/classes/mediaComponent";
import Games from "~/components/Forms/Game/Form.vue";
import MyGames from "~/components/Forms//Game/MyForm.vue";
import Data from "~/components/Media/Table/Game/Data.vue";
import MyData from "~/components/Media/Table/Game/MyData.vue";

export const useGameStore = defineStore("games", (): IStore<Game> => {
  const Type = {
    Games: "Games",
    MyGames: "MyGames",
  };
  const {
    Id: id,
    Media,
    Filter,
    MainEndpoint,
    PageIndex,
    TotalPages,
    Api,
  } = BaseStore<Game>("games");
  MainEndpoint.value = "games";
  const GameComponents = new MediaComponent(Games, Data, MediaColumns);

  const MyGameComponents = new MediaComponent(MyGames, MyData, MyMediaColumns);
  const activeComponent = shallowRef(GameComponents);

  function changeMode(): string {
    if (id.value == Type.MyGames) {
      id.value = Type.Games;
      activeComponent.value = GameComponents;
      Filter.value.MyMedia = false;
      return "Media";
    }
    else {
      id.value = Type.MyGames;
      activeComponent.value = MyGameComponents;
      Filter.value.MyMedia = true;
      return "MyMedia";
    } 
  }

  const Id = computed(() => {
    return id.value;
  });

  const ActiveComponent = computed(() => {
    return activeComponent.value;
  });

  return {
    Id,
    Media,
    Filter,
    PageIndex,
    Type,
    TotalPages,
    ActiveComponent,
    GameComponents,
    MyGameComponents,
    Api,
    changeMode,
  };
});
