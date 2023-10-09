<script setup lang="ts">
import { useGameStore } from '~/store/games';
import { MediaFilter } from '~/utils/classes/mediaFilter';

const store = useGameStore();
const mediaFilter: Ref<MediaFilter> = ref(new MediaFilter())
mediaFilter.value.PageIndex = store.PageIndex
await useAsyncData(`${store.Id}${store.PageIndex}`, () => store.Api.getMedia(store.Id, mediaFilter.value))

async function getPaginatedMedia(page: number) {
    if (page < 1) {
        page = 1;
    }
    if (page > store.TotalPages) {
        page = store.TotalPages;
    }
    mediaFilter.value.PageIndex = page;
    await store.Api.getMedia(store.Id, mediaFilter.value);
}
</script>
<template>
    <section class="sm:container mt-4 sm:px-4 mx-auto">
        <div class="w-full">
            <!-- Start coding here -->
            <div class="relative overflow-hidden bg-white rounded-b-lg shadow-md dark:bg-gray-800">
                <nav class="flex flex-row items-center justify-between p-4 space-y-3 md:space-y-0"
                    aria-label="Table navigation">
                    <span>{{ store.Id }}</span>
                    <ul class="inline-flex items-stretch -space-x-px">
                        <li>
                            <p @click="getPaginatedMedia(store.PageIndex - 1)"
                                class="flex items-center justify-center h-full py-1.5 px-3 ml-0 text-gray-500 bg-white rounded-l-lg border border-gray-300 hover:bg-gray-100 hover:text-gray-700 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-400 dark:hover:bg-gray-700 dark:hover:text-white">
                                <span class="sr-only">Previous</span>
                                <svg class="w-5 h-5" aria-hidden="true" fill="currentColor" viewBox="0 0 20 20"
                                    xmlns="http://www.w3.org/2000/svg">
                                    <path fill-rule="evenodd"
                                        d="M12.707 5.293a1 1 0 010 1.414L9.414 10l3.293 3.293a1 1 0 01-1.414 1.414l-4-4a1 1 0 010-1.414l4-4a1 1 0 011.414 0z"
                                        clip-rule="evenodd"></path>
                                </svg>
                            </p>
                        </li>
                        <li v-if="store.PageIndex > 2">
                            <p @click="getPaginatedMedia(store.PageIndex - 2)"
                                class="cursor-pointer flex items-center justify-center px-3 py-2 text-sm leading-tight text-gray-500 bg-white border border-gray-300 hover:bg-gray-100 hover:text-gray-700 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-400 dark:hover:bg-gray-700 dark:hover:text-white">
                                {{ store.PageIndex - 2 }}
                            </p>
                        </li>
                        <li v-if="store.PageIndex > 1">
                            <p @click="getPaginatedMedia(store.PageIndex - 1)"
                                class="cursor-pointer flex items-center justify-center px-3 py-2 text-sm leading-tight text-gray-500 bg-white border border-gray-300 hover:bg-gray-100 hover:text-gray-700 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-400 dark:hover:bg-gray-700 dark:hover:text-white">
                                {{ store.PageIndex - 1 }}
                            </p>
                        </li>
                        <li>
                            <p @click="getPaginatedMedia(store.PageIndex)" aria-current="page"
                                class="cursor-pointer z-10 flex items-center justify-center px-3 py-2 text-sm leading-tight border text-primary-600 bg-primary-50 border-primary-300 hover:bg-primary-100 hover:text-primary-700 dark:border-gray-700 dark:bg-gray-700 dark:text-white">
                                {{ store.PageIndex }}
                            </p>
                        </li>
                        <template v-if="store.PageIndex < store.TotalPages">
                            <li v-if="store.TotalPages > 2">
                                <p @click="getPaginatedMedia(store.PageIndex + 1)"
                                    class="cursor-pointer flex items-center justify-center px-3 py-2 text-sm leading-tight text-gray-500 bg-white border border-gray-300 hover:bg-gray-100 hover:text-gray-700 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-400 dark:hover:bg-gray-700 dark:hover:text-white">
                                    {{ store.PageIndex + 1 }}
                                </p>
                            </li>
                            <li v-if="store.TotalPages > 2 && store.PageIndex != store.TotalPages -1">
                                <p @click="getPaginatedMedia(store.TotalPages)"
                                    class="cursor-pointer flex items-center justify-center px-3 py-2 text-sm leading-tight text-gray-500 bg-white border border-gray-300 hover:bg-gray-100 hover:text-gray-700 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-400 dark:hover:bg-gray-700 dark:hover:text-white">
                                    {{ store.TotalPages }}
                                </p>
                            </li>
                        </template>
                        <li>
                            <p @click="getPaginatedMedia(store.PageIndex + 1)"
                                class="cursor-pointer flex items-center justify-center h-full py-1.5 px-3 leading-tight text-gray-500 bg-white rounded-r-lg border border-gray-300 hover:bg-gray-100 hover:text-gray-700 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-400 dark:hover:bg-gray-700 dark:hover:text-white">
                                <span class="sr-only">Next</span>
                                <svg class="w-5 h-5" aria-hidden="true" fill="currentColor" viewBox="0 0 20 20"
                                    xmlns="http://www.w3.org/2000/svg">
                                    <path fill-rule="evenodd"
                                        d="M7.293 14.707a1 1 0 010-1.414L10.586 10 7.293 6.707a1 1 0 011.414-1.414l4 4a1 1 0 010 1.414l-4 4a1 1 0 01-1.414 0z"
                                        clip-rule="evenodd"></path>
                                </svg>
                            </p>
                        </li>
                    </ul>
                </nav>
            </div>
        </div>
    </section>
</template>