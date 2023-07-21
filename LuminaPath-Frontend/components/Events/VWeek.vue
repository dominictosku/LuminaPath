<script setup lang="ts">
import { Calendar } from 'v-calendar';

const calendar: any = ref(null);

function moveToday() {
    calendar.value.move(new Date());
}

const todos = ref([
    {
        description: 'Play FFXIV',
        isComplete: false,
        dates: new Date(), // Every Friday
        color: 'blue',
    },
]);

const attributes: any = computed(() => [
    // Attributes for todos
    ...todos.value.map(todo => ({
        dates: todo.dates,
        dot: {
            color: todo.isComplete ? todo.color : 'red',
            class: todo.isComplete ? 'opacity-75' : '',
        },
        popover: {
            label: todo.description,
            visibility: 'focus'
        },
    })),
    {
        key: 'today',
        highlight: true,
        dates: new Date(),
    },
]);
</script>
<template>
    <Calendar ref="calendar" :attributes="attributes" view="weekly" expanded />
    <button class="bg-indigo-600 hover:bg-indigo-700
                     text-white font-bold w-full px-3 py-1 rounded-md" @click="moveToday">
        Today
    </button>
</template>