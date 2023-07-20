<script setup lang="ts">
import { Game, Plattforms } from "@/utils/models/games"
import { useGameStore } from "@/stores/games"
const props = defineProps({
    game: Object as PropType<Game>
})
const store = useGameStore()
const game: any= ref(props.game)
const modal: any = ref(null)
const router = useIonRouter()

function cancel() {
  modal.value.$el.dismiss(null, 'cancel');
}

async function deleteGame() {
  await store.removeGame(game.value.id)
  modal.value.$el.dismiss(null, 'cancel');
  router.back()
}

async function confirm() {
  await store.createGame(game.value)
  modal.value.$el.dismiss(null, 'cancel');
}
</script>
<template>
  <ion-content class="ion-padding">
    <ion-button id="open-modal" expand="block">Edit Game</ion-button>
    <ion-modal ref="modal" trigger="open-modal">
      <ion-header>
        <ion-toolbar>
          <ion-button @click="cancel()" slot="start">
            X
          </ion-button>
          <ion-title> Edit Game </ion-title>
        </ion-toolbar>
      </ion-header>
      <ion-content class="ion-padding">
          <form class="grid justify-center">
            <div class="border-solid border-2 border-sky-500 p-6">
              <FormKit v-model="game.name" name="Title" label="Title of game" validation="required" />
              <FormKit v-model="game.description" type="textarea" name="description" label="description" />
              <FormKit v-model="game.plattforms" type="select" name="plattform" label="Plattform" placeholder="Playstation"
              :options="Plattforms" />
              <FormKit v-model="game.genre" type="text" name="genre" label="genre" />
              <FormKit v-model="game.playtime" type="number" label="Estimated Playtime" step="1" />
              <div class="flex justify-end p-3 gap-3">
                <IonButton @click="deleteGame()" color="danger">delete</IonButton>
                <IonButton @click="cancel()" color="light">close</IonButton>
                <IonButton @click="confirm()"> Edit </IonButton>
              </div>
            </div>
          </form>
      </ion-content>
    </ion-modal>
  </ion-content>
</template>
  