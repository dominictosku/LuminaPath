<template>
  <ion-app>
    <nav-bar />
    <ion-router-outlet class="mt-16" />
  </ion-app>
</template>

<script setup lang="ts">
import NavBar from './components/NavBar.vue';
const title = ref("Welcome")
</script>
