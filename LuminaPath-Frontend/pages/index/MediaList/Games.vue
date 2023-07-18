<script setup lang="ts">
import { getGames } from '@/services/request';

const { data: games, pending, error } = await useAsyncData('games', () => getGames(), {
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