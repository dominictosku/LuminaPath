<script setup lang="ts">
</script>
<template>
  <ion-page>
    <ion-content :fullscreen="true">
      <VCalendar />
      <Drag />
      <ClientOnly>
        <VFullCalendar />
      </ClientOnly>
    </ion-content>
  </ion-page>
</template>

