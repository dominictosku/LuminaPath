<script setup lang="ts">
import { Game } from '~/utils/model/games';
import Modal from '~/components/Forms/Create.vue';
const props = defineProps({
    media: {
        type: Object as PropType<Game>,
        required: true
    }
})

const modalProps = { game: props.media.myGames, form: "myMedia", id: props.media.id }
</script>
<template>
    <tr v-if="media != undefined" @click="openModal(Modal, modalProps)">
        <td class="px-4 py-4 whitespace-nowrap">
            <div class="w-36">
                <h2 class="text-sm md:text-base truncate text-gray-800 dark:text-white ">
                    {{ media.name }}
                </h2>
                <p class="text-sm truncate font-normal text-gray-600 dark:text-gray-400">
                    {{ media.description }}
                </p>
            </div>
        </td>
        <td class="px-12 py-4 whitespace-nowrap">
            <div class="w-36 inline px-3 py-1 text-sm md:text-base
                                     rounded-full text-emerald-500 gap-x-2 bg-emerald-100/60
                                      dark:bg-gray-800">
                Completed
            </div>
        </td>
        <td class="px-4 py-4 whitespace-nowrap">
            <div class="w-20 ">
                <h4 class="text-gray-700 text-sm md:text-base dark:text-gray-200">
                    {{ media.myGames?.timeSpend }}
                </h4>
                <p class="text-gray-500 text-sm md:text-base dark:text-gray-400">
                    Main + Side
                </p>
            </div>
        </td>

        <td class="px-4 py-4 text-sm whitespace-nowrap">
            <div class="w-20 h-1.5 bg-blue-200 overflow-hidden rounded-full">
                <div class="bg-blue-500 w-2/3 h-1.5"></div>
            </div>
        </td>
    </tr>
</template>
<style scoped></style>