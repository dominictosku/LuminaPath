<script setup lang="ts">
import FullCalendar from '@fullcalendar/vue3'
import dayGridPlugin from '@fullcalendar/daygrid'
import interactionPlugin from '@fullcalendar/interaction'
const calendarOptions = {
        plugins: [ dayGridPlugin, interactionPlugin ],
        initialView: 'dayGridMonth'
}
const fullCalendar: any = ref(null)

onIonViewWillEnter(() => {
    setTimeout(function () {
        window.dispatchEvent(new Event('resize'));
    }, 1);
});
</script>
<template>
    <FullCalendar ref="fullCalendar" :options="calendarOptions" />
</template>