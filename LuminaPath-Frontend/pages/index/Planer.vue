<script setup lang="ts">
import { Calendar } from 'v-calendar';
</script>
<template>
  <ion-page>
    <ion-content :fullscreen="true">
      <EventsVWeek />
      <ion-list>
        <ion-item-sliding v-for="item in 10">
          <ion-item-options side="start">
            <ion-item-option color="success">Completed</ion-item-option>
          </ion-item-options>

          <ion-item>
            <ion-label>Todo: idk</ion-label>
          </ion-item>

          <ion-item-options side="end">
            <ion-item-option color="danger">Delete</ion-item-option>
          </ion-item-options>
        </ion-item-sliding>
      </ion-list>
      <EventsVFullCalendar class="sm:p-12" />
    </ion-content>
  </ion-page>
</template>

