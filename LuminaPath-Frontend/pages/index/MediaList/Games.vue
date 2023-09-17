<script setup lang="ts">
import { useGameStore } from '@/store/games';
const gameStore = useGameStore()
const store: any = ref(gameStore)
const ToggleMyMedia = ref(false)
const refresh = ref(false)
const formType = ref("Games")
const type : Ref<"games" | "myGames"> = ref("games")
const { data: games, pending, error } = await useAsyncData('games', () => gameStore.getMedia(), {
  lazy: true
})
const toggle = async () => {
  ToggleMyMedia.value = !ToggleMyMedia.value
  type.value = ToggleMyMedia.value ? "myGames" : "games"
  store.value =  ToggleMyMedia.value ? gameStore.MyStore : gameStore
  formType.value = store.value.Id
  await useAsyncData(type.value, () => store.value.getMedia())
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
        <Media :Store="store" :type="type" :key="refresh" />
      </div>
    </ion-content>
  </ion-page>
</template>