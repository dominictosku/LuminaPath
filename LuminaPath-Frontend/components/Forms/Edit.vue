<script setup lang="ts">
import { Game } from "~/utils/games";
defineProps({
  game: Object as PropType<Game>
})

const modal: any = ref(null)

function cancel() {
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
        <FormsGameForm :game="game" :show-delete="true" @exit="cancel" />
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
  