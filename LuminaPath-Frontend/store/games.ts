import { BaseStore } from "~/utils/classes/baseStore";
import { IStore } from "~/utils/interfaces/IBasicStore";
import { IGame } from "~/utils/interfaces/iGames";
import { MyGame } from "#imports";
import { MediaComponent } from "~/utils/classes/mediaComponent"
import Games from "~/components/Forms/GameForm.vue";
import MyGames from "~/components/Forms/MyGameForm.vue";
import Data from "~/components/Media/Table/Data.vue";
import MyData from "~/components/Media/Table/MyData.vue";

export const useGameStore = defineStore("games", (): IStore<IGame, MyGame> => {
  const Type = {
    Games: "Games",
    MyGames: "MyGames",
  };
  const {
    Id: id,
    Media,
    MyMedia,
    Mode,
    PageIndex,
    TotalPages,
    Api,
  } = BaseStore<IGame, MyGame>("games");
  const SelectedGameId: Ref<number> = ref(0);
  const GameComponents = new MediaComponent(Games, Data, MediaColumns);

  const MyGameComponents = new MediaComponent(MyGames, MyData, MyMediaColumns);
  const activeComponent = shallowRef(GameComponents);

  function changeMode(): string {
    if (Mode.value == "Media") {
      Mode.value = "MyMedia";
      id.value = Type.MyGames;
      activeComponent.value = MyGameComponents;
      return "MyMedia";
    } else {
      Mode.value = "Media";
      id.value = Type.Games;
      activeComponent.value = GameComponents;
      return "Media";
    }
  }

  const Id = computed(() => {
    return id.value;
  });

  const ActiveComponent = computed(() => {
    return activeComponent.value
  })

  return {
    Id,
    Media,
    MyMedia,
    SelectedGameId,
    PageIndex,
    TotalPages,
    ActiveComponent,
    Api,
    changeMode,
  };
});
