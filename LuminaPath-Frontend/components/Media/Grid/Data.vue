<script setup lang="ts">
import { Game } from '~/utils/games';
const props = defineProps({
    game: Object as PropType<Game>
})
const router = useIonRouter();
</script>
<template>
    <div @click="() => router.push(`/Details/Games/${props.game?.id}`, customAnimation)">
        <div class="">
            <img src="../../assets/png/Placeholder.png" class="object-cover h-48 w-96" />
        </div>
        <div class="bg-gray-100 text-black text-center h3">
            {{ game?.name }}
        </div>
        <FormsAddToList :game-id="game?.id" />
    </div>
</template>