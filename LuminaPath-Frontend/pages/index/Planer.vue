<script setup lang="ts">
</script>
<template>
  <ion-page>
    <ion-content :fullscreen="true">
      <EventsVCalendar />
      <Drag />
      <EventsVFullCalendar class="sm:p-12" />
    </ion-content>
  </ion-page>
</template>

