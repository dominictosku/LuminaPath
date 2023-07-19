<script setup lang="ts">
import { useRoute } from 'vue-router';
import { Game, Plattforms } from "@/utils/models/games"
import { useGameStore } from "@/stores/games"
const route = useRoute();
const router = useIonRouter()
const { id } = route.params ;
const store = useGameStore()
const idAsNumber: number = parseInt(id as string);
let editGame = store.getGameById(idAsNumber)
if (!editGame) {
  throw createError({ statusCode: 404, statusMessage: 'Page Not Found' })
}
const game: Ref<Game> = ref(editGame)

async function post() {
  await store.createGame(game.value)
  goBack()
}

function goBack(){
  router.back()
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
        <form class="p-3 grid justify-center">
      <div class="border-b text-center">
        <h5 class="text-lg font-bold">Add Game</h5>
      </div>
      <FormKit v-model="game.name" name="Title" label="Title of game" validation="required" />
      <FormKit v-model="game.description" type="textarea" name="description" label="description" />
      <FormKit v-model="game.plattforms" type="select" name="plattform" label="Plattform" placeholder="Playstation"
        :options="Plattforms" />
      <FormKit v-model="game.genre" type="text" name="genre" label="genre" />
      <FormKit v-model="game.playtime" type="number" label="Estimated Playtime" step="1" />
      <div class="flex justify-end p-3 gap-3">
        <IonButton @click="goBack" color="light">close</IonButton>
        <IonButton @click="post"> Add </IonButton>
      </div>
    </form>
    </ion-content>
  </ion-page>
</template>
  