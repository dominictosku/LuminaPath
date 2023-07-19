<script setup lang="ts">
</script>
<template>
  <ion-menu content-id="main-content" side="end">
    <ion-header>
      <ion-toolbar>
        <ion-title>Menu Content</ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content class="ion-padding">
      <ion-menu-toggle>
        <ion-button>Click to close the menu</ion-button>
      </ion-menu-toggle>
    </ion-content>
  </ion-menu>
    <ion-header id="main-content" class="flex">
      <ion-toolbar>
        <ion-buttons slot="end">
          <ion-menu-button></ion-menu-button>
        </ion-buttons>
        <ion-title>Menu</ion-title>
      </ion-toolbar>
    </ion-header>
</template>