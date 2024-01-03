import { BaseStore } from "~/utils/classes/baseStore";
import { type IStore } from "~/utils/interfaces/IBasicStore";
import { Game, MediaFilter } from "#imports";
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
    NextEndpoint,
    PageIndex,
    TotalPages,
    Api,
  } = BaseStore<Game>("games");
  const GameComponents = new MediaComponent(Games, Data, MediaColumns);

  const MyGameComponents = new MediaComponent(MyGames, MyData, MyMediaColumns);
  const activeComponent = shallowRef(GameComponents);
  const Filter = ref(new MediaFilter())

  function changeMode(): string {
    if (id.value == Type.MyGames) {
      id.value = Type.Games;
      activeComponent.value = GameComponents;
      NextEndpoint.value = ""
      return "Media";
    }
    else {
      id.value = Type.MyGames;
      activeComponent.value = MyGameComponents;
      NextEndpoint.value = Type.Games
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
