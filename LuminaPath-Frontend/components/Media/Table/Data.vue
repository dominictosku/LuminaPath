<script setup lang="ts">
import { Game } from '~/utils/games';
const props = defineProps({
    media: Object as PropType<Game>
})
const router = useIonRouter();
</script>
<template>
    <tr v-if="media != undefined" @click="() => router.push(`/Details/${props.media?.id}`, customAnimation)">
        <td class="px-4 py-4 text-sm font-medium whitespace-nowrap">
            <div>
                <h2 class="font-medium text-gray-800 dark:text-white ">
                    {{ media.name}}
                </h2>
                <p class="text-sm font-normal text-gray-600 dark:text-gray-400">
                    {{ media.description }}
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
                {{ media.plattforms }}
            </div>
        </td>
        <td class="px-4 py-4 text-sm whitespace-nowrap">
            <div>
                <h4 class="text-gray-700 dark:text-gray-200">
                    {{ media.playtime }}
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
                    src="../../assets/svg/user-bolt.svg" alt="">
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