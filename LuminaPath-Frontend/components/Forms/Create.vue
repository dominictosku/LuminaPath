  <script lang="ts" setup>
    import { modalController } from '@ionic/vue';
    import GameForms from "./GameForm.vue"
    import MyGameForms from "./MyGameForm.vue"
    const props = defineProps({
      formType: String
    })
  
  const forms: any = {
    GameForms,
    MyGameForms
  }
    let newGame = new Game(0, "", "", "", 0, 0)
    let newMyGame = new MyGame(0, 0, new Date(), new Date(), 0, 0, 1)
    let propGame = props.formType == "GameForms" ? newGame : newMyGame

    const confirm = () => modalController.dismiss('confirm');
  </script>
<template>
    <ion-header>
      <ion-toolbar>
        <ion-buttons slot="start">
          <ion-button color="medium" @click="cancel">Cancel</ion-button>
        </ion-buttons>
        <ion-title>Modal</ion-title>
        <ion-buttons slot="end">
          <ion-button @click="confirm" :strong="true">Confirm</ion-button>
        </ion-buttons>
      </ion-toolbar>
    </ion-header>
    <ion-content class="ion-padding">
      <ion-item>
        <div class="flex justify-center mx-auto">
            <component :is="forms[formType ?? 'GameForms']" :game="propGame" @exit="confirm" />
        </div>
      </ion-item>
    </ion-content>
  </template>
  