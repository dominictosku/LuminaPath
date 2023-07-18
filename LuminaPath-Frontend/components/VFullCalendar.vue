<script setup lang="ts">
import FullCalendar from '@fullcalendar/vue3'
import dayGridPlugin from '@fullcalendar/daygrid'
import interactionPlugin from '@fullcalendar/interaction'
const calendarOptions = {
        plugins: [ dayGridPlugin, interactionPlugin ],
        initialView: 'dayGridMonth'
}
const fullCalendar: any = ref(null)

onMounted(() => {
  let calendarApi = fullCalendar.value.getApi()
  setTimeout(() => {
    calendarApi.render()
  }, 500);
})
</script>
<template>
    <FullCalendar ref="fullCalendar" :options="calendarOptions" />
</template>