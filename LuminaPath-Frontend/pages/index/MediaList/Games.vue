<script setup lang="ts">
import { useGameStore } from '@/store/games';
import { useMyGameStore } from '~/store/myGames';
const gameStore = useGameStore()
const myStore = useMyGameStore()
const propDictionary = {
  games: {
    id: "games",
    formType: "GameForms",
    store: gameStore
  },
  myGames: {
    id: "myGames",
    formType: "MyGameForms",
    store: myStore
  }
}
const store: any = ref(gameStore)
const ToggleMyMedia = ref(false)
const refresh = ref(false)
const formType = ref("GameForms")
const { data: games, pending, error } = await useAsyncData('games', () => store.value.getMedia(), {
  lazy: true
})

const toggle = async () => {
  let type : "games" | "myGames" = "games"
  ToggleMyMedia.value = !ToggleMyMedia.value
  type = ToggleMyMedia.value ? "myGames" : "games"
  let id = propDictionary[type].id
  formType.value = propDictionary[type].formType
  store.value = propDictionary[type].store
  await useAsyncData(id, () => store.value.getMedia())
  refresh.value = !refresh.value
}
</script>
<template>
  <ion-page>
    <ion-content :fullscreen="true">
      <MediaNavigation />
      <div v-if="pending">
        <Loading />
      </div>
      <div v-else-if="error != null || games == null">
        <ErrorData />
      </div>
      <div class="m-4" v-else>
        <IonButton @click="toggle">Toggle</IonButton>
        <Media :Store="store" :Forms="formType" :key="refresh" />
      </div>
    </ion-content>
  </ion-page>
</template>