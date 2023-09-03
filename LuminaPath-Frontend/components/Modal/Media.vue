<script setup lang="ts">
</script>
<template>
    <ion-header>
    <ion-toolbar>
      <ion-buttons slot="start">
        <ion-button color="medium" @click="cancel">Cancel</ion-button>
      </ion-buttons>
      <ion-title><slot name="header"></slot></ion-title>
      <ion-buttons slot="end">
        <ion-button @click="confirm" :strong="true">Confirm</ion-button>
      </ion-buttons>
    </ion-toolbar>
  </ion-header>
  <ion-content class="ion-padding">
    <slot :exit="confirm"></slot>
  </ion-content>
</template>
<style scoped>
  ion-toast.custom-toast {
    --background: red;
    --box-shadow: 3px 3px 10px 0 rgba(0, 0, 0, 0.2);
    --color: #4b4a50;
  }
</style>
  