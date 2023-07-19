<script setup lang="ts">
import { useRoute } from 'vue-router';
import { useGameStore } from "@/stores/games"
const route = useRoute();
const { id } = route.params;
const store = useGameStore()
const idAsNumber: number = parseInt(id as string);
let editGame = store.getGameById(idAsNumber)
if (!editGame) {
  throw createError({ statusCode: 404, statusMessage: 'Page Not Found' })
}

</script>
<template>
  <ion-page>
    <ion-header>
      <ion-toolbar>
        <ion-buttons slot="start">
          <ion-back-button></ion-back-button>
        </ion-buttons>
        <ion-title>Edit Game: {{ id }} </ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content>
      <FormsEdit :game="editGame" />
    </ion-content>
  </ion-page>
</template>
  