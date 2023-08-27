<script setup lang="ts">
import { useGameStore } from '@/store/games'
const store = useGameStore()
const game = computed(() => { return store.SelectedGame})
</script>
<template>
    <ion-header>
      <ion-toolbar>
        <ion-buttons slot="start">
          <ion-back-button></ion-back-button>
        </ion-buttons>
        <ion-title> {{ game?.name }} </ion-title>
      </ion-toolbar>
    </ion-header>
    <ion-content class="ion-padding">
      <h1>{{ game?.name }} </h1>
    </ion-content>
</template>