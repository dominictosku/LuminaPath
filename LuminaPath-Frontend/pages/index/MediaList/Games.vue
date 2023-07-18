<script setup lang="ts">
import { useGameStore } from '@/stores/games';

const store = useGameStore()
const { data: games, pending, error } = await useAsyncData('games', () => store.getGames(), {
  lazy: true
})
</script>
<template>
  <ion-page>
    <ion-content>
      <MediaNavigation />
      <div v-if="pending">
        <Loading />
      </div>
      <div v-else-if="error != null">
        <ErrorData />
      </div>
      <div v-else>
        <Media />
      </div>
    </ion-content>
  </ion-page>
</template>