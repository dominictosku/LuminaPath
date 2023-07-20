<script setup lang="ts">
import { useGameStore } from "@/stores/games"
import { toastController } from '@ionic/vue';
import { Game } from "~/utils/games";
const props = defineProps({
  game: Object as PropType<Game>
})
const store = useGameStore()
const game: any = ref(props.game)
const modal: any = ref(null)
const router = useIonRouter()
const submitted = ref(false)

function cancel() {
  modal.value.$el.dismiss(null, 'cancel');
}

async function deleteGame() {
  await store.removeGame(game.value.id)
  modal.value.$el.dismiss(null, 'cancel');
  router.back()
}

async function confirm() {
  try{
    await store.createGame(game.value)
    await presentToast("Success!", 'primary')
    modal.value.$el.dismiss(null, 'cancel');
  }catch(e){
    await presentToast("Something went wrong, try again", 'danger')
    submitted.value = true
  }

  async function presentToast(message: string, color: 'primary' | 'danger') {
        const toast = await toastController.create({
          message: message,
          duration: 1500,
          position: 'bottom',
          color: color
        });

        await toast.present();
      }

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
        <FormKit class="" type="form" id="edit" :form-class="submitted ? 'hide' : 'show'" submit-label="Edit" @submit="confirm"
          :actions="false" #default="{ value }">
          <div class="grid justify-center">
            <FormKit v-model="game.name" name="name" label="Title of game" validation="required" />
            <FormKit v-model="game.description" type="textarea" name="description" label="description" />
            <FormKit v-model="game.plattforms" type="select" name="plattforms" label="Plattform" placeholder="Playstation"
              :options="Plattforms" />
            <FormKit v-model="game.genre" type="text" name="genre" label="genre" />
            <FormKit v-model="game.playtime" type="number" name="playtime" label="Estimated Playtime" step="1" />
            <div class="flex justify-end p-3 gap-3">
              <IonButton @click="deleteGame()" color="danger">delete</IonButton>
              <IonButton @click="cancel()" color="light">close</IonButton>
              <FormKit type="submit" label="Edit" />
            </div>
          </div>
          <pre wrap>{{ value }}</pre>
        </FormKit>
      </ion-content>
    </ion-modal>
  </ion-content>
</template>
<style scoped>
  ion-toast.custom-toast {
    --background: red;
    --box-shadow: 3px 3px 10px 0 rgba(0, 0, 0, 0.2);
    --color: #4b4a50;
  }
</style>
  