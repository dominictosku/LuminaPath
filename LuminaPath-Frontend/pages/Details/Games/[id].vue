<script setup lang="ts">
import { IGame } from 'utils/games';
import { useRoute } from 'vue-router';
const route = useRoute();
const { id } = route.params;
const idInt: number = parseInt(id as string);
const { data: editGame } = await useAsyncData('games/' + idInt, async () => await fetchMediaById<IGame>(idInt, "games"))

if (!editGame) {
  throw createError({ statusCode: 404, statusMessage: 'Page Not Found' })
}

</script>
<template>
  <ion-page>
    <NavigationGoBack>
      <p>Edit Game: {{ id }} </p>
    </NavigationGoBack>
    <ion-content>
      <FormsEdit :game="editGame" />
    </ion-content>
  </ion-page>
</template>
  