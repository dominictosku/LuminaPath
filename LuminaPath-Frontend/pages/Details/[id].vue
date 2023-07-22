<script setup lang="ts">
import { useRoute } from 'vue-router';
const route = useRoute();
const { id } = route.params;
const idInt: number = parseInt(id as string);
const { data: editGame, pending, error } = await useAsyncData('games/' + idInt, () => fetchGameById(idInt))

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
  