<script setup lang="ts">
import { logoAmplify } from 'ionicons/icons';
import { useUserStore } from '~/stores/user';
const router = useIonRouter();
const store = useUserStore()

async function logout(){
  await store.Logout()
  router.push('/Auth/Login')
}

</script>
<template>
  <ion-menu content-id="main-content" side="end">
    <ion-header>
      <ion-toolbar>
        <ion-title>{{ store.getUser.userName }}</ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content class="ion-padding">
      <ion-button v-if="!store.loggedIn" @Click="() => router.push(`/Auth/Login`)">Login</ion-button>
      <ion-button color="danger" v-else @Click="logout()">Logout</ion-button>
    </ion-content>
  </ion-menu>
    <ion-header id="main-content" class="flex">
      <ion-toolbar>
        <ion-buttons slot="end">
          <div v-if="!store.loggedIn">Login</div>
          <div v-else>Hello {{ store.getUser.userName }}</div>
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