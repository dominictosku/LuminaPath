<script setup lang="ts">
import { MyGame } from '~/utils/games';
import Modal from '~/components/Forms/Create.vue';
const props = defineProps({
    media: Object as PropType<MyGame>
})

let myGame = props.media
if(myGame){
    myGame.game = null;
}
const modalProps = { game: myGame, formType: "MyGames" }
</script>
<template>
    <tr v-if="media != undefined" @click="openModal(Modal, modalProps)">
        <td class="px-4 py-4 text-sm font-medium whitespace-nowrap">
            <div>
                <h2 class="font-medium text-gray-800 dark:text-white ">
                    {{ media?.game?.name }}
                </h2>
                <p class="text-sm font-normal text-gray-600 dark:text-gray-400">
                    {{ media.rating }}
                </p>
            </div>
        </td>
        <td class="px-12 py-4 text-sm font-medium whitespace-nowrap">
            <div class="inline px-3 py-1 text-sm font-normal
                                     rounded-full text-emerald-500 gap-x-2 bg-emerald-100/60
                                      dark:bg-gray-800">
                Completed
            </div>
        </td>
        <td class="px-12 py-4 text-sm font-medium whitespace-nowrap">
            <div class="inline px-3 py-1 text-sm font-normal
                                                rounded-full text-emerald-500 gap-x-2 bg-emerald-100/60
                                                dark:bg-gray-800">
                {{ media.startDate }}
            </div>
        </td>
        <td class="px-4 py-4 text-sm whitespace-nowrap">
            <div>
                <h4 class="text-gray-700 dark:text-gray-200">
                    {{ media.timeSpend }}
                </h4>
                <p class="text-gray-500 dark:text-gray-400">
                    Main + Side
                </p>
            </div>
        </td>
        <td class="px-4 py-4 text-sm whitespace-nowrap">
            <div class="flex items-center">
                <img class="object-cover w-6 h-6 -mx-1 border-2 border-white
                                                rounded-full dark:border-gray-700 shrink-0"
                    src="~/assets/icons/user-bolt.svg" alt="">
                <p class="flex items-center justify-center
                                             w-6 h-6 -mx-1 text-xs text-blue-600 bg-blue-100 border-2
                                              border-white rounded-full">
                    +4
                </p>
            </div>
        </td>

        <td class="px-4 py-4 text-sm whitespace-nowrap">
            <div class="w-48 h-1.5 bg-blue-200 overflow-hidden rounded-full">
                <div class="bg-blue-500 w-2/3 h-1.5"></div>
            </div>
        </td>
    </tr>
</template>
<style scoped></style>