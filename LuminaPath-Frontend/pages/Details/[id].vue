<template>
    <ion-page>
      <ion-header>
        <ion-toolbar>
          <ion-title>Details</ion-title>
        </ion-toolbar>
      </ion-header>
  
      <ion-content> Detail ID: {{ id }} </ion-content>
    </ion-page>
  </template>
  
  <script lang="ts">
    import { IonContent, IonHeader, IonPage, IonTitle, IonToolbar } from '@ionic/vue';
    import { defineComponent } from 'vue';
    import { useRoute } from 'vue-router';
  
    export default defineComponent({
      name: 'Detail',
      components: {
        IonContent,
        IonHeader,
        IonPage,
        IonTitle,
        IonToolbar,
      },
      setup() {
        const route = useRoute();
        const { id } = route.params;
        return { id };
      },
    });
  </script>