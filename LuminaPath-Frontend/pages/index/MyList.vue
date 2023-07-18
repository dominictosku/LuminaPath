<script setup lang="ts">
import { useGameStore } from '@/stores/games';

const store = useGameStore()
</script>
<template>
  <ion-page>
    <ion-content :fullscreen="true">
      <button @click="store.count++"> {{ store.count }} </button>
    </ion-content>
  </ion-page>
</template>