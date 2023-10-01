<script setup lang="ts">
onIonViewWillEnter(() => {
    setTimeout(function () {
        window.dispatchEvent(new Event('resize'));
    }, 1);
});
</script>
<template>
  <ion-page>
    <ion-content :fullscreen="true">
      <UIEventsVWeek/>
      <ion-list>
        <ion-item-sliding v-for="item in 4">
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
      <UIEventsVFullCalendar class="sm:p-12" />
    </ion-content>
  </ion-page>
</template>

