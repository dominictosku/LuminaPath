<script setup lang="ts">
import { Game } from '~/utils/model/games';
import { computed, PropType } from "vue";
const props = defineProps({
    game: {
        type: Object as PropType<Game>,
        required: true
    }
})

const getImage = computed(() => {
    console.log(props.game)
    if(props.game == undefined || props.game.image == null){
        return "~/assets/png/Placeholder.png";
    }
    return props.game.image.uri;
})

const router = useIonRouter();
</script>
<template>
    <div>
        <div @click="() => router.push(`/Details/Games/${props.game.id}`, customAnimation)">
            <div class="bg-white">
                <img :src="getImage" class="object-cover h-48 w-96" />
            </div>
            <div class="bg-gray-100 p-3 text-black text-center h3">
                {{ game.name }}
            </div>
        </div>
        <FormsAddToList :game="game" />
    </div>
</template>