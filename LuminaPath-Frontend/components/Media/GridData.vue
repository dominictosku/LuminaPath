<script setup lang="ts">
import { Game } from '@/utils/models/games';
import { useGameStore } from '@/stores/games'
const props = defineProps({
    game: Object as PropType<Game>
})

function loadGame(){
    if(props.game != null){
        store.setSelectedGame(props.game)
    }
}

const store = useGameStore()
</script>
<template>
    <div @click="loadGame" data-bs-toggle="modal" data-bs-target="#exampleModal">
        <div class="">
            <img src="../../assets/png/Placeholder.png" class="object-cover h-48 w-96" />
        </div>
        <div class="bg-gray-100 text-black text-center h3">
            {{ game?.name }}
        </div>
    </div>
</template>