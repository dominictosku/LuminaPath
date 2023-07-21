<script setup lang="ts">
import { Calendar } from 'v-calendar';
</script>
<template>
  <ion-page>
    <ion-content :fullscreen="true">
      <EventsVWeek />
      <ion-list>
        <ion-item-sliding>
          <ion-item>
            <ion-label>Sliding Item with End Options</ion-label>
          </ion-item>

          <ion-item-options>
            <ion-item-option>Favorite</ion-item-option>
            <ion-item-option color="danger">Delete</ion-item-option>
          </ion-item-options>
        </ion-item-sliding>

        <ion-item-sliding>
          <ion-item-options side="start">
            <ion-item-option color="success">Archive</ion-item-option>
          </ion-item-options>

          <ion-item>
            <ion-label>Sliding Item with Start Options</ion-label>
          </ion-item>
        </ion-item-sliding>

        <ion-item-sliding>
          <ion-item-options side="start">
            <ion-item-option color="success">Archive</ion-item-option>
          </ion-item-options>

          <ion-item>
            <ion-label>Sliding Item with Options on Both Sides</ion-label>
          </ion-item>

          <ion-item-options side="end">
            <ion-item-option>Favorite</ion-item-option>
            <ion-item-option color="danger">Delete</ion-item-option>
          </ion-item-options>
        </ion-item-sliding>
      </ion-list>
      <Drag />
      <EventsVFullCalendar class="sm:p-12" />
    </ion-content>
  </ion-page>
</template>

