<script setup lang="ts">
import { Game } from '@/utils/models/games';
defineProps({
    game: Object as PropType<Game>
})
</script>
<template>
    <div data-bs-toggle="modal" data-bs-target="#exampleModal">
        <div class="">
            <img src="../../assets/png/Placeholder.png" class="object-cover h-48 w-96" />
        </div>
        <div class="bg-gray-100 text-black text-center h3">
            {{ game?.name }}
        </div>
    </div>
</template>