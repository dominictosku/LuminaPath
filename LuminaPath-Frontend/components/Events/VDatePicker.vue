<template>
  <DatePicker v-model="date">
    <template #default="{ togglePopover }">
        <button type="button" class="px-3 py-2 bg-blue-500 text-sm text-white font-semibold rounded-md"
          @click="togglePopover">
          Select date
        </button>
    </template>
  </DatePicker>
</template>
  
<script>
import { DatePicker } from 'v-calendar';
import 'v-calendar/style.css';

export default {
  components: {
    DatePicker
  },
  data() {
    return {
      date: new Date(),
    };
  },
}
</script>