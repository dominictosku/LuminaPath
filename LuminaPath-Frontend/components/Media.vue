<script setup lang="ts">
import { MyGame } from '~/utils/games';
import { IMainStore, IStore } from '~/utils/interfaces/IBasicStore';
import { IGame } from '~/utils/interfaces/iGames';
const props = defineProps({
  Store: Object,
  type: String,
  Forms: String
})

type TypeMap = {
  game: IStore<IGame> & IMainStore<MyGame>;
  myGame: IStore<MyGame>;
};

// Define a function that takes a type string and returns the corresponding type
function getType<T extends keyof TypeMap>(typeName: T): TypeMap[T] {
  // You can add more logic here if needed
  return props.Store as TypeMap[T];
}

const store: any = getType("game")
const formType = props.Forms
provide('store', store)
provide('formType', formType)

const ionInfinite = (ev: any) => {
  setTimeout(() => ev.target.complete(), 500);
};

const handleRefresh = async (event: any) => {
  await store.getMedia()
  event.target.complete();
};

const isGrid = ref(false)

function changeIsGrid() {
  isGrid.value = !isGrid.value
}
</script>

<template>
  <div>
    <MediaTabs @changebool="changeIsGrid" />
    <MediaLuminaFilter />
    <ion-refresher slot="fixed" @ionRefresh="handleRefresh($event)">
      <ion-refresher-content></ion-refresher-content>
    </ion-refresher>
    <!-- Table view -->
    <div v-if="!isGrid" id="Table" class="tabcontent">
      <MediaTableLumina />
      <MediaLuminaPagination />
    </div>
    <!-- Gallery view -->
    <div v-else id="Grid" class="tabcontent">
      <MediaGridLumina />
    </div>
    <ion-infinite-scroll @ionInfinite="ionInfinite">
      <ion-infinite-scroll-content></ion-infinite-scroll-content>
    </ion-infinite-scroll>
  </div>
  </template>
  
<style scoped>
/* Style the tab content */
.tabcontent {
  -webkit-animation: fadeEffect 1s;
  animation: fadeEffect 1s;
}

/* Fade in tabs */
@-webkit-keyframes fadeEffect {
  from {
    opacity: 0;
  }

  to {
    opacity: 1;
  }
}

@keyframes fadeEffect {
  from {
    opacity: 0;
  }

  to {
    opacity: 1;
  }
}
</style>
  