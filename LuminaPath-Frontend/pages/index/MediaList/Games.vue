<script setup lang="ts">
import { useGameStore } from '@/store/games';
const store = useGameStore()
provide('store', store)
const { data: games, pending, error } = await useAsyncData('games', () => store.getMedia(), {
  lazy: true
})
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
        <Media />
      </div>
    </ion-content>
  </ion-page>
</template>