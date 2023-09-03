<script setup lang="ts">
import { IGame } from 'utils/games';
import { useRoute } from 'vue-router';
import Modal from '~/components/Forms/Edit.vue';
const route = useRoute();
const { id } = route.params;
const idInt: number = parseInt(id as string);
const { data: editGame } = await useAsyncData('games/' + idInt, async () => await fetchMediaById<IGame>(idInt, "games"))

if (!editGame) {
  throw createError({ statusCode: 404, statusMessage: 'Page Not Found' })
}
const modalProps = {game: editGame}

</script>
<template>
  <ion-page>
    <NavigationGoBack>
      <p>Edit Game: {{ id }} </p>
    </NavigationGoBack>
    <ion-content>
      <ion-button @click="openModal(Modal, modalProps)" expand="block">Edit Game</ion-button>
    </ion-content>
  </ion-page>
</template>
  