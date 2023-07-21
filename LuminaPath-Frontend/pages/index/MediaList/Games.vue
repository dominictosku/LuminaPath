<script setup lang="ts">
import { useGameStore } from '@/stores/games';
const store = useGameStore()
const { data: games, pending, error } = await useAsyncData('games', () => store.getGames(), {
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