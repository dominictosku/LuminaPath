<script setup lang="ts">
import { useGameStore } from '@/stores/games';
const store = useGameStore()
</script>
<template>
    <section class="container px-4 mx-auto">
        <div class="flex flex-col mt-6">
            <div class="-mx-4 -my-2 overflow-x-auto sm:-mx-6 lg:-mx-8">
                <div class="inline-block min-w-full py-2 align-middle md:px-6 lg:px-8">
                    <div class="overflow-hidden border border-gray-200 dark:border-gray-700 md:rounded-lg">
                        <table class="min-w-full divide-y divide-gray-200 dark:divide-gray-700">
                            <thead class="bg-gray-50 dark:bg-gray-800">
                                <tr>
                                    <th scope="col" class="py-3.5 px-4 text-sm font-normal text-left
                                        rtl:text-right text-gray-500 dark:text-gray-400">
                                        <button class="flex items-center gap-x-3 focus:outline-none">
                                            <span>Title</span>

                                        </button>
                                    </th>

                                    <th scope="col" class="media-th">
                                        Status
                                    </th>

                                    <th scope="col" class="media-th">
                                        Plattform
                                    </th>

                                    <th scope="col" class="media-th">
                                        Playtime
                                    </th>

                                    <th scope="col" class="media-th">Users</th>

                                    <th scope="col" class="media-th">
                                        Progress
                                    </th>
                                </tr>
                            </thead>
                            <tbody class="bg-white divide-y divide-gray-200 dark:divide-gray-700 dark:bg-gray-900">
                               <MediaTableData v-for="item in store.GamesList" :title="item.name" />
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
    </section>
</template>
<style scoped>
tbody:hover {
    background-color: #1d3145;
    cursor: pointer;
}
</style>