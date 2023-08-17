  <script lang="ts" setup>
    import { modalController } from '@ionic/vue';
    let newGame = new Game(0, "", "", "", 0, 0)

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
            <FormsGameForm :game="newGame" @exit="confirm" />
        </div>
      </ion-item>
    </ion-content>
  </template>
  