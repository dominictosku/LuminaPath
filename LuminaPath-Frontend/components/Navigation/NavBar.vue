<script setup lang="ts">
import { logoAmplify } from 'ionicons/icons';
const router = useIonRouter();
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
        <ion-button @Click="() => router.push(`/Login`)">Login</ion-button>
        <ion-button>Click to close the menu</ion-button>
      </ion-menu-toggle>
    </ion-content>
  </ion-menu>
    <ion-header id="main-content" class="flex">
      <ion-toolbar>
        <ion-buttons slot="end">
          <ion-menu-button></ion-menu-button>
        </ion-buttons>
        <IonButton router-link="/Home" fill="clear">
          <ion-title>
            <IonIcon :icon="logoAmplify"></IonIcon>
            LuminaPath
          </ion-title>
        </IonButton>
      </ion-toolbar>
    </ion-header>
</template>