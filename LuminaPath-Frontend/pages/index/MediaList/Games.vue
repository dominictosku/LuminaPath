<script setup lang="ts">
import { useGameStore } from '@/store/games';
const store = useGameStore()
const refresh = ref(false)
const { data: games, pending, error } = await useAsyncData('games', () => store.Api.getMedia(store.Id), {
  lazy: true
})
const toggle = async () => {
  store.changeMode()
  await useAsyncData(store.Id, () => store.Api.getMedia(store.Id))
  refresh.value = !refresh.value
}
</script>
<template>
  <ion-page>
    <ion-content :fullscreen="true">
      <MediaToolsNavigation />
      <div v-if="pending">
        <UIEventsLoading />
      </div>
      <div v-else-if="error != null || games == null">
        <UIErrorData/>
      </div>
      <div class="m-4" v-else>
        <IonButton @click="toggle">Toggle</IonButton>
        <Media :key="refresh" />
      </div>
    </ion-content>
  </ion-page>
</template>