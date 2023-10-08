<script setup lang="ts">
import { PropType } from 'nuxt/dist/app/compat/capi';
import { Game } from '~/utils/model/games';
const props = defineProps({
    game: {
        type: Object as PropType<Game>,
        required: true
    }
})
const router = useIonRouter();
</script>
<template>
    <div>
        <div @click="() => router.push(`/Details/Games/${props.game.id}`, customAnimation)">
            <div class="bg-white">
                <img src="../../assets/png/Placeholder.png" class="object-cover h-48 w-96" />
            </div>
            <div class="bg-gray-100 p-3 text-black text-center h3">
                {{ game.name }}
            </div>
        </div>
        <FormsAddToList :game="game" />
    </div>
</template>