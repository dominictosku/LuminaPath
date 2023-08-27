<script setup lang="ts">
import { useMyGameStore } from '~/store/myGames';
const store = useMyGameStore()
provide('store', store)
provide('formType', "MyGameForms")
const { data: games, pending, error } = await useAsyncData('myGames', () => store.getMedia(), {
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